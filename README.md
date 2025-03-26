![image](https://github.com/user-attachments/assets/9ef52693-fe95-480c-86ad-b2af1422879b)

## Возможности ✨

- **Защищенная Аутентификация** 🔐 - Уникальные ключи гарантируют доступ только авторизованным студентам
- **Различные Типы Вопросов** 🧩 - Поддержка вопросов с одиночным выбором, множественным выбором и свободным текстом
- **Интеграция Медиа** 🖼️ - Улучшение вопросов изображениями для визуального обучения
- **Ограничение По Времени** ⏱️ - Настраиваемые ограничения времени для каждого вопроса
- **Красивые Результаты** 📊 - Чёткие, анимированные экраны результатов с подробной обратной связью
- **Панель Администратора** 🛠️ - Генерация уникальных ключей, просмотр статистики и анализ производительности

## Начало Работы 🚀

### Требования

- Windows 10 или новее
- .NET Framework 4.8 или новее
- Интернет-соединение для коммуникации с API

### Установка

1. Клонировать репозиторий
   ```
   git clone https://github.com/enoughdrama/testing-module.git
   ```

2. Открыть решение в Visual Studio 2019 или новее

3. Обновить базовый URL API в `App.config` если необходимо
   ```xml
   <appSettings>
     <add key="ApiBaseUrl" value="http://your-api-url:port/api" />
   </appSettings>
   ```

4. Скомпилировать и запустить приложение

### Настройка Бэкенда

Приложение требует сервер Node.js:

1. Перейти в директорию API
   ```
   cd Server
   ```

2. Установить зависимости
   ```
   npm install
   ```

3. Создать файл `.env` со следующими переменными:
   ```
   PORT=2996
   MONGO_URI=your_mongodb_connection_string
   JWT_SECRET=your_jwt_secret
   JWT_EXPIRE=30d
   JWT_COOKIE_EXPIRE=30
   ```

4. Запустить сервер
   ```
   npm start
   ```

## Использование 📋

### Опыт Студента

1. **Вход** - Введите ваше полное имя и уникальный ключ, предоставленный преподавателем
2. **Прохождение Тестирования** - Ответьте на вопросы в пределах ограничения времени
3. **Просмотр Результатов** - Получите немедленную обратную связь по вашей производительности

### Опыт Администратора

1. Доступ к интерфейсу администратора по адресу `http://localhost:2996/admin`
2. Генерация и управление уникальными ключами для студентов
3. Просмотр результатов тестов и статистики
4. Создание и редактирование вопросов

## Архитектура 🏗️

- **WPF Клиент** - Чистый и отзывчивый интерфейс Windows
- **Node.js API** - RESTful бэкенд-сервисы
- **MongoDB** - Гибкое хранилище данных

## Скриншоты 🖥️

![image](https://github.com/user-attachments/assets/8d50c029-0d6d-4c0f-ba3c-12d6006aa6d2)
- Админ панель (Общая статистика)

![image](https://github.com/user-attachments/assets/d719d127-7abc-4bb4-94c7-f4ef30ceac1a)
- Админ панель (Управление ключами доступа)

![image](https://github.com/user-attachments/assets/ce1e189a-782f-4903-828a-210b984d6687)
- Админ панель (Управление вопросами к тесту)

![image](https://github.com/user-attachments/assets/b7009840-d376-4165-b1ea-2ba19c2b2717)
- Админ панель (Выборочная статистика)

![image](https://github.com/user-attachments/assets/ddba13de-bf38-4431-ba13-f4a0290ab39a)
- Приложение (Окно авторизации)

![image](https://github.com/user-attachments/assets/6a78ff5c-efc6-40fb-b0d4-5a44db7b76bf)
- Приложение (Окно тестирования)

## Безопасность 🔒

- JWT аутентификация для безопасного доступа к API
- Уникальные ключи для предотвращения несанкционированного доступа
- Хэширование паролей с помощью bcrypt

---

Сделано с ❤️ используя C# и Node.js
