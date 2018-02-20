using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Drawing;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Freezer.Core;
using System.Text.RegularExpressions;
using System.Timers;

namespace ucit_bot_console
{
    class Program
    {
        static TelegramBotClient Bot;

        static void Main(string[] args)
        {
            string key_path = @"d:\YandexDisk\UCIT2017\rasp\key";
            string key = System.IO.File.ReadAllText(key_path);

            Bot = new TelegramBotClient(key);
            Bot.OnMessage += BotOnMessageReceived; // подписка на события сообщения от пользователей
          //  Bot.OnCallbackQuery += BotOnCallbackQueryRecived;

            Bot.StartReceiving();
            Console.ReadLine();
            Bot.StopReceiving();


        }
   

        private async static void Schedule(ChatId telegram_chat_id, bool user_request)
        {   
            string url = "http://ucit.ru/rasp.php";//страница для парсинга
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/532.5 (KHTML, like Gecko) Chrome/4.0.249.89 Safari/532.5";
            request.KeepAlive = true;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(1251));
            string content = "";
            content = reader.ReadToEnd();
            
            string pattern = @"Текущая неделя №.+\d\D+февраля 2018";//шаблон до которого надо отрезать лишнее от начала файла
            Regex regex = new Regex(pattern);
            Match index = regex.Match(content);//Определяем позицию строки, до которой нужно все отрезать  
            content = content.Substring(index.Index);//отрезать с начала до текущего индекса
                                                    
            pattern = @"<div align=right>";//шаблон после которой нужно все отрезать лишнее до конца файла
            regex = new Regex(pattern);
            index = regex.Match(content);//Определяем позицию строки, после которой нужно все отрезать
            content = content.Remove(index.Index);//отрезать от индекса до конца

            //если в тексте встречается текст, который нам не нужен, вырезаем его
            content = content.Replace("</div>", "");
            content = content.Replace("&nbsp;", "");

            // Путь для сохранения файлов и название файлов
            string file_path = @"d:\YandexDisk\UCIT2017\rasp\";
            string file_name = @"shedold.html";
            string image_name = @"shedold.png";
            FileInfo shedold = new FileInfo(file_path + file_name);
            if (!shedold.Exists)
            {
                System.IO.File.WriteAllText(file_path + file_name, "");
            }
            string file = System.IO.File.ReadAllText(file_path + file_name);

            if ((content != file) || user_request )
            {
                System.IO.File.WriteAllText(file_path + file_name, content, Encoding.GetEncoding(65001));

                var screenshotJob = ScreenshotJobBuilder.Create(file_path + file_name)
               .SetBrowserSize(900, 400)
               .SetCaptureZone(CaptureZone.FullPage) // Зона захвата
               .SetTrigger(new WindowLoadTrigger()); // Set when the picture is taken
                System.IO.File.WriteAllBytes(file_path + image_name, screenshotJob.Freeze());
                                               
                Uri shed_pic = new Uri("https://2.downloader.disk.yandex.ru/preview/4e4ab7e525ff874072a7e70256915fa71aa3190f26ce88cbfe7e7f71bc90bcc3/inf/ydqD8sJdOF9-4IOwMMPZ_oDJ0MQIT5sZ9JcvkjC1strfbrUa-XqtUSkYMy9LZuZ_OjpUXi85R9t7OERNDvQkAA%3D%3D?uid=0&filename=shedold.png&disposition=inline&hash=&limit=0&content_type=image%2Fpng&tknv=v2&size=XXL&crop=0");//ССылка на картинку через яндекс диск
                string caption_text = "Снимок расписания ";
                DateTime thisDay = DateTime.Today;
                await Bot.SendPhotoAsync(telegram_chat_id, new FileToSend(shed_pic), caption: caption_text + thisDay.ToString("d"));             

            }
        }

        private async static void VkCheck(ChatId telegram_chat_id)
        {

            /*
             * //VK

 string key_path = @"d:\YandexDisk\UCIT2017\rasp\token_vk";
 string token_vk = System.IO.File.ReadAllText(key_path);
$vk_wall_id="108679504";
$vk_text = "https://vk.com/wall-";
             // Удаляем минус у ID групп, что мы используем выше (понадобится для ссылки).
$group_id = preg_replace("/-/i", "", $vk_wall_id);
 // Количество записей, которое нам нужно получить.
$count_item = "1";

 // Получаем информацию, подставив все данные выше.
$api = file_get_contents("https://api.vk.com/api.php?oauth=1&method=wall.get&owner_id=-{$vk_wall_id}&count={$count_item}&v=5.69&access_token={$token_vk}");

// Преобразуем JSON-строку в массив
$wall = json_decode($api);

// Получаем номер записи
$post_id = $wall->response->items[0]->id ;

$file = file_get_contents('vkold'); //читаем файл со старой информацию
if($wall!=$file)
{
	file_put_contents('vkold', $wall);
	//echo iconv("windows-1251", "UTF-8", $wall); 
	//$som='Обновилось расписание ucit.ru/rasp.php';

	$sendToTelegram = fopen("{$urltelegram}{$telegram_group_id}&text={$vk_text}{$group_id}_{$post_id}","r");
	$sendToTelegram = fopen("{$urltelegram}{$telegram_chanal_id}&text={$vk_text}{$group_id}_{$post_id}","r");
	//$sendToTelegram = fopen("{$urltelegram}{$telegram_chat_id}&text={$vk_text}{$group_id}_{$post_id}","r");
}

             
             
             */
            Console.WriteLine();
        }
        
      /*  private static async void BotOnCallbackQueryRecived(object sender, CallbackQueryEventArgs e)
        {
            string buttonText = e.CallbackQuery.Data;
            string name = $"{e.CallbackQuery.From.FirstName} {e.CallbackQuery.From.LastName}";
            Console.WriteLine($"{name} Нажал кнопку {buttonText}");
            await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Вы нажали кнопку {buttonText}");
           
        }
        */
        private static async  void BotOnMessageReceived(object sender, MessageEventArgs e)
        {
            var message = e.Message;

            if (message == null || message.Type != MessageType.TextMessage) //проверка , если сообщение пустое или не тестовое, то выйти
                return;

            string name = $"{message.From.FirstName} {message.From.LastName}";
            Console.WriteLine($"Сообщение из чата {message.Chat.Title} id {message.Chat.Id}  от {name} id {message.From.Id} : '{message.Text}'");

            switch (message.Text)
            {
                case "/start":
                    string text =
                        @"Список команд:
/start - запуск бота
/rasp - вывести расписание
Или отправить текст 'Расписание'
/keyboard - вывод клавиатуры";
                    await Bot.SendTextMessageAsync(message.Chat.Id, text);
                    break;

                case "/rasp":
                    await Bot.SendTextMessageAsync(message.Chat.Id, $"{name} Смотри и учись!");
                    Schedule(message.Chat.Id, true);
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
                    //await Bot.SendTextMessageAsync(message.Chat.Id, "Действие не опознано, попробуй ещё");
                    break;
            }

            // обработка keyboard кнопок
            if (message.Text.ToLower() == "расписание")
            {

                await Bot.SendTextMessageAsync(message.Chat.Id, $"{name} Смотри и учись!");
                Schedule(message.Chat.Id, true);
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
