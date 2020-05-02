using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections;
using System.IO;
using System.Threading;

namespace WhatsappChatToCsv
{
    static class Program
    {
        static IWebDriver driver = new ChromeDriver();
        static string groupName;
        static Queue buffer = new Queue(5000);
        static string dataDirectory = "c:\\temp\\data";

        static void Main(string[] args)
        {
            groupName = string.Empty;

            driver.Navigate().GoToUrl("https://web.whatsapp.com");
            Console.Write("Press Enter If you have authenticated the whatsapp: ");
            Console.ReadLine();

            Console.Write("Eneter contact/group name: ");
            groupName = Console.ReadLine();


            driver.FindElement(By.XPath("//*[@id=\"side\"]/div[1]/div/label/div/div[2]")).Click();
            IWebElement ser = driver.FindElement(By.CssSelector("#side > div.rRAIq > div > label > div > div._2S1VP.copyable-text.selectable-text"));
            ser.SendKeys(groupName);

            Thread.Sleep(3000);

            driver.FindElement(By.XPath("//span[@title='" + groupName + "']")).Click();

            driver.FindElement(By.CssSelector("#main > div._3zJZ2")).Click();

            Thread.Sleep(5000);

            Thread t1 = new Thread(KeepScrolling);
            t1.Start();


            Thread t2 = new Thread(RetriveData);
            t2.Start();

            Thread t3 = new Thread(WriteToFile);
            t3.Start();

            Thread.Sleep(1000000000);



        }
        private static void WriteToFile()
        {
            string fileName = Path.Combine(dataDirectory, DateTime.Now.Ticks + "-" + groupName + ".csv");

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            while (true)
            {
                try
                {
                    while (buffer.Count > 0)
                    {
                        //Console.WriteLine("buffer count: " + buffer.Count);
                        string data = (string)buffer.Dequeue();
                        File.AppendAllText(fileName, data);
                    }
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.Message);
                }

            }
        }
        private static string FormatSender(string sender)
        {
            sender = sender.Trim();
            sender = "\"" + sender;
            int index = sender.LastIndexOf(']');
            sender = sender.Insert(index + 1, "\"");
            sender = sender.Insert(index + 2, ",");
            sender = sender.Replace("[", "").Replace("]", "");
            sender = sender.Remove(sender.Length - 1, 1);

            return sender;
        }

        private static string FormatMessage(string message)
        {
            message = message.Replace("\n", " ");
            message = "\"" + message + "\"";
            return message;

        }
        private static IWebElement DoElementExists(IWebElement webele, string xpath)
        {
            try
            {
                return webele.FindElement(By.XPath(xpath));
            }
            catch (Exception)
            {

                return null;
            }
        }

        private static void KeepScrolling()
        {
            for (int i = 0; i >= 0; i++)
            {
                try
                {
                    Actions actions = new Actions(driver);
                    actions.SendKeys(Keys.PageUp).Perform();
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
        }

        private static void RetriveData()
        {

            int processed = 0;

            while (true)
            {
                try
                {
                    IWebElement webElement = driver.FindElement(By.CssSelector("#main > div._3zJZ2 > div > div > div._9tCEa"));

                    var message_in = webElement.FindElements(By.XPath("//*[@class='vW7d1 _1T1d2 message-in focusable-list-item' or @class='vW7d1 message-in focusable-list-item' or @class='vW7d1 _1T1d2 message-out focusable-list-item' or @class='vW7d1 message-out focusable-list-item']"));

                    string xpath1_msg = ".//div/div/div/div[@class='_3Usvm copyable-text']/div[@class='_3zb-j']/span[@class='_3FXB1 selectable-text invisible-space copyable-text']/span";
                    string xpath2_msg = ".//div/div/div/div[@class='copyable-text']/div/span[@class='_3FXB1 selectable-text invisible-space copyable-text']/span";


                    string xpath1_sender = ".//div/div/div/div[@class='_3Usvm copyable-text']";
                    string xpath2_sender = ".//div/div/div/div[@class='copyable-text']";

                    for (int i = message_in.Count - processed - 1; i >= 0; i--)
                    {
                        IWebElement webele = message_in[i];

                        IWebElement ele;

                        if ((ele = DoElementExists(webele, xpath1_msg)) != null)
                        {
                            string msg = ele.Text;
                            string sender = string.Empty;
                            IWebElement senderEle;
                            if ((senderEle = DoElementExists(webele, xpath1_sender)) != null)
                            {
                                sender = senderEle.GetAttribute("data-pre-plain-text");
                            }

                            msg = FormatMessage(msg);
                            sender = FormatSender(sender);

                            //Console.WriteLine(String.Format("{0,-50}\t{1,-10}", sender, msg));
                            string row = sender + "," + msg + "\n";
                            buffer.Enqueue(row);
                            //File.AppendAllText(fileName, row);
                        }
                        else if ((ele = DoElementExists(webele, xpath2_msg)) != null)
                        {
                            string msg = ele.Text;
                            string sender = string.Empty;
                            IWebElement senderEle;
                            if ((senderEle = DoElementExists(webele, xpath2_sender)) != null)
                            {
                                sender = senderEle.GetAttribute("data-pre-plain-text");
                            }
                            msg = FormatMessage(msg);
                            sender = FormatSender(sender);
                            //Console.WriteLine(String.Format("{0,-50}\t{1,-10}", sender, msg));
                            string row = sender + "," + msg + "\n";
                            //File.AppendAllText(fileName, row);
                            buffer.Enqueue(row);
                        }

                    }

                    processed = message_in.Count;
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.Message);
                }
            }

        }

    }
}

