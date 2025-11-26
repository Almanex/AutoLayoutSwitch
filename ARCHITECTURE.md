# Структура проекта

```
AutoLayoutSwitch/
├── src/                          # Исходный код
│   ├── Program.cs               # Точка входа, главный цикл
│   ├── KeyboardHook.cs          # Низкоуровневый перехват клавиатуры
│   ├── LayoutWatcher.cs         # Логика определения и переключения раскладки
│   ├── SettingsForm.cs          # GUI окно настроек
│   ├── Settings.cs              # Управление конфигурацией
│   ├── Win32.cs                 # Win32 API декларации
│   ├── icon.ico                 # Иконка приложения
│   └── AutoLayoutSwitch.csproj  # Файл проекта
├── .gitignore                   # Git ignore файл
├── LICENSE                      # MIT лицензия
├── README.md                    # Документация
├── CONTRIBUTING.md              # Руководство по вкладу
├── CHANGELOG.md                 # История изменений
└── build.ps1                    # Скрипт сборки релиза
```

## Архитектура

### Компоненты

#### Program.cs
- Создание скрытого окна для обработки сообщений
- Регистрация иконки в системном трее
- Регистрация глобальной горячей клавиши
- Управление жизненным циклом приложения
- Обработка автозапуска через Registry

#### KeyboardHook.cs
- Установка низкоуровневого хука клавиатуры (`WH_KEYBOARD_LL`)
- Фильтрация инжектированных событий (от `SendInput`)
- Возврат флага подавления клавиши

#### LayoutWatcher.cs
- Определение текущей раскладки через `GetKeyboardLayout`
- Преобразование VK кода в символ через `ToUnicodeEx`
- Применение эвристик для определения неправильной раскладки
- Переключение раскладки через TSF (`ITfInputProcessorProfileMgr`)
- Исправление текста через `SendInput` (Backspace + повторный ввод)
- Хранение VK кодов для корректного повторного ввода

#### SettingsForm.cs
- Windows Forms GUI для настроек
- Поддержка High DPI
- Захват комбинаций клавиш для горячей клавиши
- Сохранение настроек в JSON

#### Settings.cs
- Сериализация/десериализация JSON
- Управление настройками:
  - `PlaySound` - звук при переключении
  - `AutoStart` - автозапуск
  - `HotKeyVk` / `HotKeyModifiers` - горячая клавиша
  - `Exceptions` - список исключений

#### Win32.cs
- P/Invoke декларации для Win32 API
- COM интерфейсы для TSF (Text Services Framework)
- Константы и структуры

### Поток данных

```
Нажатие клавиши
    ↓
KeyboardHook (WH_KEYBOARD_LL)
    ↓
LayoutWatcher.OnKeyPress()
    ↓
Определение символа (ToUnicodeEx)
    ↓
Применение эвристик
    ↓
Переключение раскладки? (Да/Нет)
    ↓ (Да)
SwitchToLanguage() → TSF/PostMessage
    ↓
RewriteLastWord() → SendInput
    ↓
Подавление текущей клавиши (return true)
```

### Эвристики

1. **Проверка гласных** (слова ≥3 символа)
   - Если слово без гласных текущей раскладки → переключить

2. **Подозрительные символы**
   - `[`, `]`, `;`, `'` в английском слове → русская раскладка

3. **Недопустимые начальные символы**
   - `ь`, `ъ`, `ы` в начале русского слова → английская раскладка

4. **Апостроф в начале**
   - `'` в начале английского слова → русская раскладка (буква `э`)

5. **Диапазоны символов**
   - Латиница в русской раскладке → переключить
   - Кириллица в английской раскладке → переключить

### Технические решения

#### Переключение раскладки
Используется двухуровневый подход:
1. **TSF (Text Services Framework)** - основной метод
   - `ITfInputProcessorProfileMgr::ActivateProfile` с флагом `TF_IPPMF_FORSESSION`
   - Работает глобально для всей сессии
2. **PostMessage** - fallback
   - `WM_INPUTLANGCHANGEREQUEST` для активного окна

#### Исправление текста
1. Сохранение VK кодов набранных символов
2. Отправка N-1 Backspace (если подавляем текущую клавишу)
3. Повторный ввод символов через `SendInput` с сохранёнными VK кодами
4. Подавление исходной клавиши через возврат `true` из хука

#### Предотвращение циклов
- Проверка флага `LLKHF_INJECTED` в хуке
- Игнорирование событий от `SendInput`

#### High DPI
- `Application.SetHighDpiMode(HighDpiMode.SystemAware)`
- `AutoSize` с `Padding` для кнопок

## Конфигурация

### settings.json
Расположение: `%LOCALAPPDATA%\AutoLayoutSwitch\settings.json`

```json
{
  "PlaySound": true,
  "AutoStart": true,
  "HotKeyVk": 123,
  "HotKeyModifiers": 4,
  "Exceptions": ["php", "css", "url", "jpg", "png"]
}
```

### Логи
Расположение: `%LOCALAPPDATA%\AutoLayoutSwitch\log.txt`
- Автоматическая ротация при 1 МБ
- Логируются все переключения и ошибки

## Сборка и публикация

### Требования
- .NET 8.0 SDK
- Windows 10/11

### Команды

```powershell
# Разработка
dotnet build

# Запуск
dotnet run

# Релиз
.\build.ps1
```

### Публикация на GitHub

1. Создайте релиз на GitHub
2. Загрузите `AutoLayoutSwitch.exe` из папки `release/`
3. Добавьте описание из `CHANGELOG.md`
