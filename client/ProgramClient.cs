using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace client_server_2._0
{
    class ProgramClient
    {

        static void Main()
        {
            string jsonMesgToSend;
            const string ip = "127.0.0.1"; //задаем ip
            const int port = 8080;//задаем порт, в который клиент будет стучаться, чтобы подключиться к серверу

            var tcpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            int chosenCommand = ReadCommand();//идем в метод, считывающий, чё хочет пользователь
            jsonMesgToSend = ChooseCommand(chosenCommand);//получаем строку, которую будем отсылать серверу
            
            var data = Encoding.UTF8.GetBytes(jsonMesgToSend);//кодируем строку для отправки серверу
            tcpSocket.Connect(tcpEndPoint);//подключаемся к выбранному эндпоинту
            tcpSocket.Send(data);//отсылаем строку
            
            var buffer = new byte[256];
            var size = 0;
            var answer = new StringBuilder();

            do
            {
                size = tcpSocket.Receive(buffer);
                answer.Append(Encoding.UTF8.GetString(buffer, 0, size));//получаем ответ от сервера, декодируем
            }
            while (tcpSocket.Available > 0);

            if (!answer.ToString().Contains("{"))//проверяем формат ответа. если есть { , то это строка со всеми челами из списка
                Console.WriteLine("___Server's answer: " + answer.ToString());//если это просто строка, то выводим, че там говорит сервер
            else
            ShowListOfNames(answer.ToString());//показывает список людей из файла

            //отключить и закрыть соединение
            tcpSocket.Shutdown(SocketShutdown.Both);
            tcpSocket.Close();

            //постоянно спрашивать пользователя ввести команду 
            Console.WriteLine("\nPress Esc for exit or any key to continue...");
            while (Console.ReadKey().Key != ConsoleKey.Escape)
                Main();
        }

        public static int ReadCommand()
        {
            int command = 0;
            //вовыодим пользователю возможные варианты действий
            Console.WriteLine("________________________________________" + /*попытка улучшить читаемость текста в консоли*/
                "\nChoose the option: \n 1-Add \n 2-Change \n 3-Delete \n 4-Output all");
            string input = Console.ReadLine();//считываем, чё он ввёл
            try
            {
                command = int.Parse(input);//пытаемся преобразовать в число
                if (command < 0 || command > 4)//проверка, что введенное число находится в диапазоне чисел доступных команд
                {
                    Console.WriteLine("Incorrect number!");//выбрасываем ошибку, что он дурак
                    command = 0;//обнуляем переменную с введенным числом, чтобы потом сервер понимал, что эту команду не надо обрабатывать
                }
            }
            catch { Console.WriteLine("Incorrect input!"); }
            
            return command;//возвращаем результат работы метода
        }

        public static string ChooseCommand(int cmd)//в зависимости от числа, которое приходит на вход методу, идем по одному из кейсов
        {
            string jsonMes = "";//сюда кладем строку, которую надо передать серверу

            switch (cmd)//везде в результате отработки методов в jsonMes записывается строка JSON, содержащая данные взависимости от выбранной пользователем команды
            {
                case 1:
                    Console.WriteLine("Add person:");
                    jsonMes = ReadNewPerson();
                    break;
                case 2:
                    jsonMes = ChangePersonData();
                    break;
                case 3:
                    jsonMes = DeletePerson();
                    break;
                case 4:
                    jsonMes = OutputAll();
                    break;
                case 0:
                    jsonMes = IncorrectCommand();
                    break;
            }
            return jsonMes;//сформированная строка передается на выход для отсылки серверу
        }


        public static string ReadNewPerson()
        {
            int newAge = 0;
            //ввод перс.данных
            Console.WriteLine("Family name:");
            string newFamily = Console.ReadLine();
            Console.WriteLine("Name:");
            string newName = Console.ReadLine();
            Console.WriteLine("Age:");
            string jsonMessage;

            try 
            { 
                newAge = int.Parse(Console.ReadLine());//пытаемся преобразовать в число то, что ввёл пользователь
                if (newAge < 0 || newAge >= 123)//проверка корректности возраста
                    {
                    Console.WriteLine("Age can not be negative, zero or more than 123.");
                    newFamily = "//Incorrect input";//нужно для обработки сервером, чтобы такую команду не обрабатывать и не записывать в файл
                    }
            }
            catch 
            { 
                Console.WriteLine("Incorrect number!");
                newFamily = "//Incorrect input";//аналогично строке в try
            }
            
            Message newPerson = new Message("Add", newFamily, newName, newAge);
            jsonMessage = ConvertMessageToJSON(newPerson);//закидываем данные о человеке в json

            return jsonMessage;
            }

        public static string ChangePersonData() 
        {
            int persNum;
            string message;
            string persData;
            Console.WriteLine("Enter a number of person to change:");//спрашиваем, кого надо изменить
            try 
            { 
                persNum = int.Parse(Console.ReadLine());//проверка, что введено число
                if (persNum <= 0)//проверка, что число нормальное
                {
                    Console.WriteLine("The number cannot be less than 1.");
                    message = IncorrectCommand();//сообщение серверу, что команду обрабатывать не надо
                }
                else//если число нормальное
                {
                    persData = ReadNewPerson();//запрашивает новые данные человека, записывает в строку JSON
                    Message changePerson = JsonConvert.DeserializeObject<Message>(persData);//полученную строку разбираем
                    changePerson.Command = "Change";//меняем команду, потому что ReadNewPerson() возвращает команду Add, а это метод на изменение данных
                    //костыльный костыль, чтобы как-то передать выбранный номер человека:
                    changePerson.FamilyName += "+" + persNum + "/";//к фамилии добавляется введенный пользаком номер человека на изменение
                    message = ConvertMessageToJSON(changePerson);//закидываем в JSON
                }
            }
            catch //если пользак ввел номер человека буквами
            {
                Console.WriteLine("This is not a number!");
                message = IncorrectCommand(); 
            }
            return message;//строка с командой и данными человека либо строка, что команда некорректна и обрабатывать не надо
        }

        public static string DeletePerson()
        {
            int number;
            string jsonMessage;

            Console.WriteLine("Enter number of person to delete:");//кого удаляем?
            try 
            { 
                number = int.Parse(Console.ReadLine());//опять проверка на дурака
                if (number < 0)
                {
                    Console.WriteLine("\nNumber can't be negative!");
                    jsonMessage = IncorrectCommand();
                }
                else//если  норм число
                {
                    Message newPerson = new Message("Delete", number.ToString(), "", 0);//номер человека на удаление записываем в поле фамилии
                    jsonMessage = ConvertMessageToJSON(newPerson);
                }
            }
            catch 
            { 
                Console.WriteLine("incorrect number!");
                jsonMessage = IncorrectCommand();
            }

            return jsonMessage;
        }

        public static string OutputAll()
        {
            Message message = new Message("ShowAll", "", "", 0);//передаем команду показать список людей
            string msg = ConvertMessageToJSON(message);
            Console.WriteLine();
            return msg;
        }
        public static string ConvertMessageToJSON(Message message)//на вход принимает экземпляр класса Message
        {
            return JsonConvert.SerializeObject(message).ToString(); //конвертирует в JSON, потом в строку и возвращает как результат работы метода
        }

        static void ShowListOfNames(string str)
        {
            if (str == null)// если пришла пустая строка или файл вообще не был создан
                Console.WriteLine("No names.");
            else//если есть имена
            {
                string[] outputList = str.Split(new[] { "+++" }, StringSplitOptions.None);//раскидываем строку в массив. разделяем по +++
                for (int i = outputList.Length - 1; i >= 0; i--)//проходим циклом по каждой строке отдельно. одна строка - один чел
                {
                    string line = outputList[i];
                    try
                    {
                        if (line != "")//если не пустая. На случай, чтобы прога не падала, если кто-то ручками в файл добавил пустую строку
                        {
                            int number = int.Parse(line.Substring(0, line.IndexOf(' ')));//выделяем порядковый номер человека
                            string persJSON = line.Substring(line.IndexOf('{'), outputList[i].IndexOf('}') - 1);//выделить JSON из всей строки
                            Person person = JsonConvert.DeserializeObject<Person>(persJSON);//разобрать строку в экземпляр класса. чтобы был доступ к каждому полю
                            Console.WriteLine(number + " - " + person.FamilyName + " " + person.Name + "," + person.Age + " y.o.");
                        }
                    }
                    catch {Console.WriteLine("Oops, somethng went wrong.");}
                }
            }
        }

        static string IncorrectCommand()//для невалидной команды. чтобы сервер не обрабатывал
        {
            Message newPerson = new Message("invalidCommand", "", "", 0);
            return ConvertMessageToJSON(newPerson).ToString();//конвертирует экземпляр класса в JSON и возвращает как крезультат работы метода
        }
    }

    class Message
    {
        public string Command { get; set; } //команда, чё надо сделать с человеком. 
        //фио, возраст:
        public string FamilyName { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }

        public Message(string command, string familyName, string name, int age)//конструктор
        {
            Command = command;
            FamilyName = familyName;
            Name = name;
            Age = age;
        }

    }

    class Person//класс нужен, чтобы при выводе всех записанных людей можно было распарсить данные, т.к. от сервера приходит строка json
    {
        public string FamilyName { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }

        public Person(string p_familyName, string p_name, int p_age)
        {
            FamilyName = p_familyName;
            Name = p_name;
            Age = p_age;
        }
    }
}
