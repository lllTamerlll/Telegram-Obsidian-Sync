# Telegram-Obsidian-Sync (v1.0)

Простой и надежный Self-hosted мост для мгновенной пересылки заметок из Telegram в ваш локальный Obsidian Vault. Работает через Cloudflare Workers для стабильного соединения.

---

## Шаг 1: Создание Telegram бота

1. Напишите [@BotFather](https://t.me/BotFather) и создайте нового бота командой `/newbot`.
<img width="500" height="983" alt="image" src="https://github.com/user-attachments/assets/b5782e18-5191-4c98-90a7-72b05dff8c42" />

    
2. Сохраните полученный **API Token**.

## Шаг 2: Настройка прокси (Cloudflare Worker)

_Это необходимо для стабильной работы API без блокировок._

1. Зайдите в панель управления [Cloudflare](https://dash.cloudflare.com/).
    
2. Перейдите в **Workers & Pages** -> **Create application** -> **Create Worker**.
    
3. Вставьте следующий код в редактор:
```JavaScript
export default {
  async fetch(request) {
    const url = new URL(request.url);
    if (url.pathname.startsWith('/bot')) {
      const targetUrl = 'https://api.telegram.org' + url.pathname + url.search;
      const newRequest = new Request(targetUrl, request);
      return fetch(newRequest);
    }
    return new Response('Proxy is working!', { status: 200 });
  }
};
```
4. Нажмите **Deploy** и скопируйте адрес вашего воркера (будет похож на `https://имя.workers.dev`).

## Шаг 3: Запуск и привязка

1. Скачайте последнюю версию из [Releases] (или скомпилируйте проект сами).
    
2. Запустите `.exe` файл.
    
3. Введите данные по запросу:
    
    - **Token**: Ваш API токен бота.
        
    - **Proxy URL**: Ссылка на ваш Cloudflare Worker (с приставкой `https://`).
        
    - **Path**: Полный путь к папке в Obsidian, куда будут падать заметки.
        
4. **Важно:** Напишите боту любое сообщение в Telegram. Программа увидит ваш ID и привяжет его как единственно верный (админский).

---

## Как это работает?

- **Текст**: Просто напишите сообщение — бот создаст `.md` файл с датой и тегом `#telegram_inbox`.
    
- **Фото**: Отправьте фото с подписью — бот сохранит текст подписи с пометкой `[Фото]`.
    
- **Безопасность**: Бот игнорирует сообщения от всех, кроме первого привязавшегося пользователя.

---

## Стек технологий

- **Язык**: C# / .NET 8.0
    
- **Библиотека**: [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot)
    
- **Инфраструктура**: Cloudflare Workers (JS)
