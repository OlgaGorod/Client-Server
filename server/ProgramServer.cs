using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;


namespace Server
{
    class Program
    {
        static void Main()
        {
            const string ip = "127.0.0.1";//задаем ip
            const int port = 8080;//задаем тот же порт, что на клиенте. сюда клиент и будет стучаться
            //аналогично с клиентом
            var tcpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.Bind(tcpEndPoint);
            tcpSocket.Listen(5);//создаем очередь. максимально 5. если сервер обрабатывает одну из заявок, а в это время другие клиенты
            //тоже чё-то шлют, то они будут вставать в эту очередь. но у нас тут всего 1 клиент

            while (true)
            {
                var listener = tcpSocket.Accept();//если кто-то стучится, он принимает сообщение
                var buffer = new byte[256];
                var size = 0;
                var data = new StringBuilder();
                string message;

                do
                {
                    size = listener.Receive(buffer);
                    data.Append(Encoding.UTF8.GetString(buffer, 0, size));//получили сообщение
                }
                while (listener.Available > 0);

                Console.WriteLine(data);//выводим в консоль. в принципе, это не нужно, но если убрать, сервер будет просто молча работать

                if (CheckNewData(data.ToString()))//если пройдена проверка на валидность присланных клиентом данных
                {
                    string strPers = ConvertFromJSON(data.ToString());//получаем строку перс.данных
                    message = Selecter(data.ToString(), strPers);//передаем в Selecter и исходную полученную строку, и строку только с пер.данными

                    listener.Send(Encoding.UTF8.GetBytes(message));

                    listener.Shutdown(SocketShutdown.Both);//выключить соединение
                    listener.Close();//закрыть соединение
                }
                else
                    listener.Send(Encoding.UTF8.GetBytes("An error occuired"));//если пришло сообщение с командой incorrectCommand
            }
        }
        static string SaveNewPerson(string newPers)
        {
            {
                int counter;//переменная, чтобы нумеровать список людей
                string lastLine;
                try
                {
                    lastLine = File.ReadAllLines("D:/test.txt").Last();//считывает последнюю строку из файла
                    //если последняя строчка пустая, то всё крэшится (мне было лень это обрабатывать, потому пустой строки быть не может, если только ни чьи шаловливые ручки не полезут в файл самостоятельно её добавить)
                }
                catch
                {
                    lastLine = "0 {}";//задает строку с 0 номером. нужно, чтобы потом добавляемые люди нумеровались с единицы
                }
                
                try
                    {
                        counter = int.Parse(lastLine.Substring(0, lastLine.IndexOf(' ')));//считываем номер последней строки
                        counter++;
                    
                    //записываем всё в одну строку, чтобы потом закинуть в файл:
                        string strForSave = counter.ToString() + " " + newPers+" ";//счетчик, введенные перс.данные и переход на новую строку. переход на новую строку только если в файле уже что-то записано
                    if (counter > 1)
                        strForSave = Environment.NewLine + strForSave;

                    //хорошо бы не хардкодить путь к файлу, но это уже совсем другая история
                    System.IO.File.AppendAllText("D:/test.txt", strForSave);//записывает в файл, к которому указан путь, данные из переменной strForSave. если файл ещё не создан, то создает этот файл и записывает
                    return "Success!";//информационное сообщение, которое потом отошлётся клиенту

                    }
                catch
                    {
                        return "An error occuired!";
                    }

            }
        }
        static string ChangePerson(string input) 
        {
            string message ;
            int persNumber=0;
            int startMarker;//для обработки того самого костыльного костыля
            int stopMarker;//тоже
            string strMessage;
            string newLine;

            Message msg = JsonConvert.DeserializeObject<Message>(input);//разбираем полученную строку на экземпляр класса

            startMarker = msg.FamilyName.IndexOf("+");//определяем индекс в строке фамилии. чтобы вычленить номер человека для изменения
            stopMarker = msg.FamilyName.IndexOf("/");//номер чела будет между startMarker и stopMarker

            try
            { 
                persNumber = int.Parse(msg.FamilyName.Substring(startMarker+1, stopMarker-startMarker-1));//получаем номер чела для изменения
            }
            catch
            {
                Console.WriteLine("Не удалось преобразовать в число номер выбранного чела");//тоже невозможно, если никто руками не полезет в файл
            }

            msg.FamilyName = msg.FamilyName.Substring(0, startMarker);//из строки, где записана фамилия и номер, получаем только фамилию
            strMessage = JsonConvert.SerializeObject(msg);
            string personInJSON = ConvertFromJSON(strMessage);

            try
            {
                string[] arrayOfPersons = File.ReadAllLines("D:/test.txt");//считываем в массив все строки из файла. каждый элемент массива - строка отдельного чела
                if (arrayOfPersons.Length < persNumber)//если пользак хочет изменить данные чела, которого нет в файле вообще
                    message = "List doesn't include this person";//записываем текст ошибки, который потом пошлем клиенту
                else
                {
                    File.Delete("D:/test.txt");//удаляем файл 

                    foreach (var line in arrayOfPersons)//проходимся по массиву, чтобы найти нужного чела
                    {
                        int lineNumber = int.Parse(line.Substring(0, line.IndexOf(' ')));
                        if (lineNumber == persNumber)//когда нашли совпадающий номер чела
                            newLine = persNumber.ToString() + " " + personInJSON;//записываем номер чела и новые перс.данные
                        else
                            newLine = line;//остальные строки переписываем без изменений

                        if (File.Exists("D:/test.txt"))//пишем данные с новой строки, если файл уже существует (для 2 и последующих строк)
                            newLine = Environment.NewLine + newLine;
   
                        System.IO.File.AppendAllText("D:/test.txt", newLine);//записываем строку в файл (если файл ещё не создан, создаем и записываем)
                    }

                    message = "Person's data was changed.";//информационное сообщение
                }
            }
            catch { message = "File is not found"; }//информац. сообщение, если пользак ещё не ввел ни одного человека, а уже хочет чё-то изменить

            return message;//информац.сообщение. "успешно" или текст ошибки
        }

        static string DeletePerson(int num)
        {
            int persNum;
            string message = "";
            string newLine;
            string marker = num.ToString() + " {";//ограничиваем правый край, чтобы при получении 1 он не удалял всех: 1,11,12,13 и т.д. чтобы удалял только одну строку
            try
            {
                string[] arrayOfPersons = File.ReadAllLines("D:/test.txt");
                
                foreach (var line in arrayOfPersons) //проверка, что есть человек с номером, который хочет удалить\изменить пользователь
                {
                    int stopMarkerNumb = line.IndexOf("{");//индекс фигурной скобки в строке, чтобы выделить номер человека
                    persNum = int.Parse(line.Substring(0, stopMarkerNumb-1));
                   
                    if ((num > persNum) || (num<1))
                        message = "List has no such number!";
                    else
                    if (stopMarkerNumb != num)
                    {
                        File.Delete("D:/test.txt");

                        foreach (var persLine in arrayOfPersons)
                        {
                            if (!persLine.Contains(marker) && persLine != null)//если строка не содержит заданного номера, то записывается в файл
                            {
                                if (!File.Exists("D:/test.txt"))
                                    newLine = persLine;
                                else
                                    newLine = Environment.NewLine + persLine;

                                System.IO.File.AppendAllText("D:/test.txt", newLine);
                            }
                        }
                        message = "Person's data was deleted.";//сообщение об успехе операции
                    }
                }
            }
            catch { message = "File is not found"; }
        
            return message;
        }

        static string GetAllNames() 
        {
            string strList = "";
            try
            {
                string[] arrayOfPersons = File.ReadAllLines("D:/test.txt");//считывает все строки из файла
                    foreach (string item in arrayOfPersons.ToList<string>())//формирует строку, куда записывает все имена с разделителем +++
                        strList = item + "+++" + strList;//причем записывает в обратном порядке
            }
            catch { strList = "The list of names is empty or does not exist."; }//если считать строки не удалось

            return strList;//возвращает всю эту строку с именами, чтобы отправить клиенту
        }
        static string ConvertFromJSON(string msg) 
        {
            Message message = JsonConvert.DeserializeObject<Message>(msg);//из полученной строки собираем экзампляр класса Message
            Person pers = new Person(message.FamilyName, message.Name, message.Age);//создаем экз.класса Person с полученными перс.данными
            string str = JsonConvert.SerializeObject(pers).ToString();
            return str;//возвращаем строку данных человека
        }

        static bool CheckNewData(string d)//проверка данных в полученном сообщении
        {
            Message message = JsonConvert.DeserializeObject<Message>(d);//парсим полученную строку в экземпляр класса Message
            Person pers = new Person(message.FamilyName, message.Name, message.Age);//создаем экземпляр класса Person с данными, пришедшими от клиента
            if (pers.FamilyName.Equals("//Incorrect input"))
                return false;
            else
                return true;
        }
        static string Selecter(string inputData,string strPersData) 
        {
            string infoMessage ="";//сюда запишем ответ от сервера
            Message message = JsonConvert.DeserializeObject<Message>(inputData);//парсим строку в экземпляр класса
            string command = message.Command;//чтобы выделить команду

            switch (command)//в зависимости от команды сервер чё-то делает
            {
                case "Add":
                    {
                        infoMessage = SaveNewPerson(strPersData);
                        break;
                    }
                case "Change": 
                    {
                        infoMessage = ChangePerson(strPersData);
                        break;
                    }
                case "Delete": 
                    {
                        try
                        {
                            Person person = JsonConvert.DeserializeObject<Person>(ConvertFromJSON(strPersData));//разбираем строку в экземпляр класса
                            int persNumber = int.Parse(person.FamilyName);//получаем номер чела, которого надо удалить
                            infoMessage = DeletePerson(persNumber); //вызываем метод удаления, на вход передаем номер чела
                        }
                        catch
                        {
                            infoMessage = "An error occurred during the deletion process.";
                        }
                        break;
                    }
                case "ShowAll": 
                    {
                        infoMessage = GetAllNames();
                        break;
                    }
                case "invalidCommand":
                    {
                        infoMessage = "The server cannot process an invalid command!";
                        break;
                    }

            }
            return infoMessage;//возвращаем сообщение. если всё ок, то пишем, что ок, если ошибка, то возвращаем ошибку. либо передаем строку со всеми именами
        }
    }

    class Message
    {
        public string Command { get; set; }
        public string FamilyName { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }

        public Message(string command, string familyName, string name, int age)
        {
            Command = command;
            FamilyName = familyName;
            Name = name;
            Age = age;
        }

    }
    class Person
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

