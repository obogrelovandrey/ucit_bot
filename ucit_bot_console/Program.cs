using System;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Freezer.Core;
using System.Text.RegularExpressions;


namespace ucit_bot_console
{
    class Program
    {
        static TelegramBotClient Bot;

        static void Main(string[] args)

        {
            // int v2 = int.Parse(ConfigurationManager.AppSettings["Переменная2"]);

           // string file_path = ConfigurationManager.AppSettings["file_path"];
            
            string key = ConfigurationManager.AppSettings["telegram_token"];
           
            Bot = new TelegramBotClient(key);
            Bot.OnMessage += BotOnMessageReceived; // подписка на события сообщения от пользователей

            string telegram_chat_id = ConfigurationManager.AppSettings["telegram_chat_id"];
            string vk_wall_id = ConfigurationManager.AppSettings["vk_wall_id"];
            int sleep_time = int.Parse(ConfigurationManager.AppSettings["sleep_time"]);
            
            string pattern_group_1 = ConfigurationManager.AppSettings["pattern_group_1"];//конец шаблона до которого надо отрезать лишнее от начала файла
            string pattern_group_2 = ConfigurationManager.AppSettings["pattern_group_2"];//конец шаблона до которого надо отрезать лишнее от начала файла
            string pattern_group_3 = ConfigurationManager.AppSettings["pattern_group_3"];//конец шаблона до которого надо отрезать лишнее от начала файла

            string url_png_group_1 = ConfigurationManager.AppSettings["url_png_group_1"];//ССылка на картинку через яндекс диск";
            string url_png_group_2 = ConfigurationManager.AppSettings["url_png_group_2"];//ССылка на картинку через яндекс диск";
            string url_png_group_3 = ConfigurationManager.AppSettings["url_png_group_3"];//ССылка на картинку через яндекс диск";
            Bot.StartReceiving();
           
            while (true)
            {
                VkCheck(telegram_chat_id, vk_wall_id);
                ScheduleCheckAndSend(telegram_chat_id, false, pattern_group_1, url_png_group_1);
                ScheduleCheckAndSend(telegram_chat_id, false, pattern_group_2, url_png_group_2);
                ScheduleCheckAndSend(telegram_chat_id, false, pattern_group_3, url_png_group_3);
                Thread.Sleep(sleep_time);
            }            
            //Console.ReadLine();
            //Bot.StopReceiving();  
            }
        private static string ScheduleGet(string url, string pattern_start, string pattern_end, string[]for_replace)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/532.5 (KHTML, like Gecko) Chrome/4.0.249.89 Safari/532.5";
            request.KeepAlive = true;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(1251));
            string content = "";
            content = reader.ReadToEnd();

           // pattern_start = @"Текущая неделя №.+\d\D+февраля 2018";//шаблон до которого надо отрезать лишнее от начала файла
            Regex regex = new Regex(pattern_start);
            Match index = regex.Match(content);//Определяем позицию строки, до которой нужно все отрезать  
            content = content.Substring(index.Index);//отрезать с начала до текущего индекса

            //pattern_end = @"<div align=right>";//шаблон после которой нужно все отрезать лишнее до конца файла
            regex = new Regex(pattern_end);
            index = regex.Match(content);//Определяем позицию строки, после которой нужно все отрезать
            content = content.Remove(index.Index);//отрезать от индекса до конца

            //если в тексте встречается текст, который нам не нужен, вырезаем его
            foreach (string element in for_replace)
                                            content = content.Replace(element,"");
           
            return content;
        }
        private static bool IsScheduleSaved(string new_content, string file_path, string file_name, bool is_user_request)
        {
            string ext_html = @".html";
            string ext_png = @".png";
            FileInfo shedold = new FileInfo(file_path + file_name + ext_html);
            if (!shedold.Exists)
            {
                System.IO.File.WriteAllText(file_path + file_name + ext_html, "");
            }
            string old_file = System.IO.File.ReadAllText(file_path + file_name + ext_html);

            if ((new_content != old_file) || is_user_request)
            {
                System.IO.File.WriteAllText(file_path + file_name + ext_html, new_content, Encoding.GetEncoding(65001));

                int browser_height = int.Parse(ConfigurationManager.AppSettings["browser_height"]);
                int browser_widht = int.Parse(ConfigurationManager.AppSettings["browser_widht"]);

                var screenshotJob = ScreenshotJobBuilder.Create(file_path + file_name+ ext_html)
               .SetBrowserSize(browser_widht, browser_height)
               .SetCaptureZone(CaptureZone.VisibleScreen) // Зона захвата
               .SetTrigger(new WindowLoadTrigger()); // Set when the picture is taken
                System.IO.File.WriteAllBytes(file_path + file_name + ext_png, screenshotJob.Freeze());
            return true; 
            }
            return false;

        }
        private async static void ScheduleCheckAndSend(ChatId telegram_chat_id, bool is_user_request, string pattern_group, string get_url_png) //telegram_chat_id - в какой чат отправлять расписание по умолчанию; is_user_request - не сравнивать со старым файлом. Путь передается переменной. 
        {
            string file_path = ConfigurationManager.AppSettings["file_path"];
            string url = ConfigurationManager.AppSettings["url"];//страница для парсинга            
            string pattern = @"Текущая неделя №.+\d\D+";//начало шаблона до которого надо отрезать лишнее от начала файла
            string pattern_start = pattern + pattern_group;
            string pattern_end = ConfigurationManager.AppSettings["pattern_end"];//шаблон после которой нужно все отрезать лишнее до конца файла
            pattern_end = pattern_end.Replace("&lt;", "<");
            pattern_end = pattern_end.Replace("&gt;", ">");
            string[] for_replace = { "</div>", "&nbsp;" };//если в тексте встречается текст, который нам не нужен, вырезаем его
            string content = ScheduleGet(url, pattern_start, pattern_end, for_replace);

            string file_name = pattern_group;
            if (file_name == "")
                {
                file_name = @"shedold";
                }
            //string get_url_png = ConfigurationManager.AppSettings["url_png"];//ССылка на картинку через яндекс диск";
            string url_png = get_url_png.Replace("&amp;", "&");//заменить символы ;

            if (IsScheduleSaved(content, file_path, file_name, is_user_request))
            {

                Uri shed_pic = new Uri(url_png);
                string caption_text = "Снимок расписания ";
                DateTime thisDay = DateTime.Today;

                Message x = await Bot.SendPhotoAsync(telegram_chat_id, new FileToSend(shed_pic), caption: caption_text + thisDay.ToString("d")); ;

                if (x.Caption == caption_text + thisDay.ToString("d"))
                {
                    Console.WriteLine("Отправка расписания с сайта " + file_name);
                }
                else Console.WriteLine("Хмм, попробуй еще раз, картинка с расписанием не отправляется");
            }

        }
        private async static void ScheduleCheckAndSend(ChatId telegram_chat_id, bool is_user_request) //telegram_chat_id - в какой чат отправлять расписание по умолчанию; is_user_request - не сравнивать со старым файлом. Путь берет из файла config 
        {
            string file_path = ConfigurationManager.AppSettings["file_path"];
            string url = ConfigurationManager.AppSettings["url"];//страница для парсинга
            string pattern_group = ConfigurationManager.AppSettings["pattern_group"];//конец шаблона до которого надо отрезать лишнее от начала файла
            string pattern = @"Текущая неделя №.+\d\D+";//начало шаблона до которого надо отрезать лишнее от начала файла
            string pattern_start = pattern + pattern_group;
            string pattern_end = ConfigurationManager.AppSettings["pattern_end"];//шаблон после которой нужно все отрезать лишнее до конца файла
            pattern_end = pattern_end.Replace("&lt;", "<");
            pattern_end = pattern_end.Replace("&gt;", ">");
            string[] for_replace = { "</div>", "&nbsp;" };//если в тексте встречается текст, который нам не нужен, вырезаем его
            string content = ScheduleGet(url, pattern, pattern_end, for_replace);

            string file_name = @"shedold";
            string get_url_png = ConfigurationManager.AppSettings["url_png"];//ССылка на картинку через яндекс диск";
            string url_png = get_url_png.Replace("&amp;", "&");//заменить символы ;

            if (IsScheduleSaved(content, file_path, file_name, is_user_request))
            {

                Uri shed_pic = new Uri(url_png);
                string caption_text = "Снимок расписания ";
                DateTime thisDay = DateTime.Today;

                Message x = await Bot.SendPhotoAsync(telegram_chat_id, new FileToSend(shed_pic), caption: caption_text + thisDay.ToString("d")); ;

                if (x.Caption == caption_text + thisDay.ToString("d"))
                {
                    Console.WriteLine("Отправка расписания с сайта");
                }
                else Console.WriteLine("Хмм, попробуй еще раз, картинка с расписанием не отправляется");
            }

        }
        private  async static void VkCheck(ChatId telegram_id, string vk_wall_id) //куда отправлять сообщение и за какой стеной в ВК следить 
        {               
            string token_vk = ConfigurationManager.AppSettings["token_vk"];
            string vk_pattern_url = "https://vk.com/wall-"; 
             // Количество записей, которое нам нужно получить.
            string count_item = "1";
            // Получаем информацию, подставив все данные выше.
            Uri uri = new Uri ($"https://api.vk.com/api.php?oauth=1&method=wall.get.xml&owner_id=-"+vk_wall_id+"&count="+count_item+"&v=5.69&access_token="+token_vk); 
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/532.5 (KHTML, like Gecko) Chrome/4.0.249.89 Safari/532.5";
            request.KeepAlive = true;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(1251));
            string new_post_id = reader.ReadToEnd();
                           
            string pattern_id = "<id>(\\w+)[0-9]";
            Regex regex = new Regex(pattern_id);
            Match index = regex.Match(new_post_id);
            new_post_id = index.ToString().Replace("<id>", "");

            string file_path = ConfigurationManager.AppSettings["file_path"];           
            string file_name = @"vk_last_check"; //имя файла в котором будет сохранятся номер последней записи
           

            FileInfo vkold = new FileInfo(file_path + file_name);
            if (!vkold.Exists)
            {
                System.IO.File.WriteAllText(file_path + file_name, "");
            }
            string old_file = System.IO.File.ReadAllText(file_path + file_name);

            if (new_post_id != old_file)
            {
                System.IO.File.WriteAllText(file_path + file_name, new_post_id);
                Message x = await Bot.SendTextMessageAsync(telegram_id, vk_pattern_url + vk_wall_id + "_" + new_post_id);

                if (x.Text == vk_pattern_url + vk_wall_id + "_" + new_post_id)
                {
                    Console.WriteLine("Обновилась запись в группе ВК");
                   
                }
                    else Console.WriteLine("Ошибка обновления ВК");
            }
          }   
        private static async  void BotOnMessageReceived(object sender, MessageEventArgs e)
        {
            var message = e.Message;

            if (message == null || message.Type != MessageType.TextMessage) //проверка , если сообщение пустое или не тестовое, то выйти
                return;

            string name = $"{message.From.FirstName} {message.From.LastName}";
            Console.WriteLine($"Сообщение из чата {message.Chat.Title} id {message.Chat.Id}  от {name} id {message.From.Id} : '{message.Text}'");

            switch (message.Text.ToLower())
            {
                case "/start":
                    string text =
                        @"Список команд:
/start - запуск бота
/rasp - вывести расписание
/keyboard - вывод клавиатуры
Еще я понимаю фразы:
'Привет бот' - поприветсовать бота
'Расписание' - вывести расписание
'ucit.ru' - ссылка на расписание
'Группа VK' - ссылка на группу в VK
'Я.диск' - ссылка на Яндекс диск
 ";

                    await Bot.SendTextMessageAsync(message.Chat.Id, text);
                    break;

                case "/rasp" :
                    await Bot.SendTextMessageAsync(message.Chat.Id, $"{name} Смотри и учись!");
                    ScheduleCheckAndSend(message.Chat.Id, true);
                    break;

                case "/inline":
                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {new[]
                    {
                        InlineKeyboardButton.WithUrl("Сайт ucit.ru", "http://ucit.ru/rasp.php"),
                         InlineKeyboardButton.WithUrl("Группа VK", "https://vk.com/prog_ucit")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("Расписание","http://ucit.ru/rasp.php"),
                         InlineKeyboardButton.WithUrl("Я.Диск","https://yadi.sk/d/zk4sn3pAptaXz")  
                    }
                    });
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Выберите пункт меню", replyMarkup: inlineKeyboard);
                    break;

                case "/keyboard":
                    var replyKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            new KeyboardButton ("Расписание"),
                            new KeyboardButton("ucit.ru")
                        },
                        new[]
                        {
                            new KeyboardButton ("Группа VK"),
                            new KeyboardButton("Я.Диск")
                         }
                     });
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Какой ответ? ", ParseMode.Default, false, false, 0, replyMarkup: replyKeyboard);
                    break;
                default:
                    break;
            }

            // обработка keyboard кнопок
            if (message.Text.ToLower() == "расписание")
            {

                await Bot.SendTextMessageAsync(message.Chat.Id, $"{name} Смотри и учись!");
                ScheduleCheckAndSend(message.Chat.Id, true);
               
            }
            if (message.Text.ToLower() == "ucit.ru")
            {
                await Bot.SendTextMessageAsync(message.Chat.Id, $"{name} Ну, за расписанием!");
                await Bot.SendTextMessageAsync(message.Chat.Id, "http://ucit.ru/rasp.php");
                            }
            if (message.Text.ToLower() == "группа vk")
            {

                await Bot.SendTextMessageAsync(message.Chat.Id, $"{name} Идем в VK!");
                await Bot.SendTextMessageAsync(message.Chat.Id, "https://vk.com/prog_ucit");
            }
            if (message.Text.ToLower() == "я.диск")
            {
                await Bot.SendTextMessageAsync(message.Chat.Id, $"{name} Срочно в Облако!");
                await Bot.SendTextMessageAsync(message.Chat.Id, "https://yadi.sk/d/zk4sn3pAptaXz");
            }
            if (message.Text.ToLower() == "привет бот")
            {
                await Bot.SendTextMessageAsync(message.Chat.Id, $"{name} И тебе привет! Напиши /start ия покажу что умею.");
                
            }

        }
    }
}
