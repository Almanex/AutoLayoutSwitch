using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AutoLayoutSwitch
{
    public class LayoutWatcher
    {
        private Win32.ITfInputProcessorProfileMgr? _profileMgr;
        private string _logPath;
        private bool _enabled = true;
        private Settings _settings;
        private StringBuilder _currentWord = new StringBuilder();
        private List<int> _currentWordVkCodes = new List<int>(); // Store VK codes
        private readonly HashSet<char> _enVowels = new HashSet<char> { 'a', 'e', 'i', 'o', 'u', 'y', 'A', 'E', 'I', 'O', 'U', 'Y' };
        private readonly HashSet<char> _ruVowels = new HashSet<char> { 'а', 'е', 'ё', 'и', 'о', 'у', 'ы', 'э', 'ю', 'я', 'А', 'Е', 'Ё', 'И', 'О', 'У', 'Ы', 'Э', 'Ю', 'Я' };
        private readonly Dictionary<char, char> _enToRu = new Dictionary<char, char>
        {
            // Top row
            ['q'] = 'й', ['w'] = 'ц', ['e'] = 'у', ['r'] = 'к', ['t'] = 'е', ['y'] = 'н', ['u'] = 'г', ['i'] = 'ш', ['o'] = 'щ', ['p'] = 'з', ['['] = 'х', [']'] = 'ъ',
            ['Q'] = 'Й', ['W'] = 'Ц', ['E'] = 'У', ['R'] = 'К', ['T'] = 'Е', ['Y'] = 'Н', ['U'] = 'Г', ['I'] = 'Ш', ['O'] = 'Щ', ['P'] = 'З', ['{'] = 'Х', ['}'] = 'Ъ',
            // Home row
            ['a'] = 'ф', ['s'] = 'ы', ['d'] = 'в', ['f'] = 'а', ['g'] = 'п', ['h'] = 'р', ['j'] = 'о', ['k'] = 'л', ['l'] = 'д', [';'] = 'ж', ['\''] = 'э',
            ['A'] = 'Ф', ['S'] = 'Ы', ['D'] = 'В', ['F'] = 'А', ['G'] = 'П', ['H'] = 'Р', ['J'] = 'О', ['K'] = 'Л', ['L'] = 'Д', [':'] = 'Ж', ['"'] = 'Э',
            // Bottom row
            ['z'] = 'я', ['x'] = 'ч', ['c'] = 'с', ['v'] = 'м', ['b'] = 'и', ['n'] = 'т', ['m'] = 'ь', [','] = 'б', ['.'] = 'ю', ['/'] = '.',
            ['Z'] = 'Я', ['X'] = 'Ч', ['C'] = 'С', ['V'] = 'М', ['B'] = 'И', ['N'] = 'Т', ['M'] = 'Ь', ['<'] = 'Б', ['>'] = 'Ю', ['?'] = ','
        };
        private readonly HashSet<string> _ruShortWhitelist = new HashSet<string>
        {
            "не","то","на","по","да","но","же","ли","и","в","с","тд","тд.","т.п","т.п.","тп","тп.","итд","и тд","и т.п"
        };
        
        public bool Enabled { get => _enabled; set => _enabled = value; }

        [DllImport("user32.dll")]
        private static extern int GetKeyboardLayoutList(int nBuff, [Out] IntPtr[]? lpList);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        public LayoutWatcher(Settings settings)
        {
            _settings = settings;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, "AutoLayoutSwitch");
            Directory.CreateDirectory(dir);
            _logPath = Path.Combine(dir, "log.txt");

            try
            {
                Type? type = Type.GetTypeFromCLSID(new Guid("33C53A50-F456-4884-B049-85FD643ECFED"));
                if (type != null)
                {
                    _profileMgr = (Win32.ITfInputProcessorProfileMgr?)Activator.CreateInstance(type);
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to init TSF: {ex.Message}");
            }
        }

        public bool OnKeyPress(int vkCode, int scanCode)
        {
            if (!_enabled || _profileMgr == null) return false;

            // Обработка Backspace (VK_BACK = 0x08)
            if (vkCode == 0x08) 
            {
                if (_currentWord.Length > 0)
                {
                    _currentWord.Length--;
                    if (_currentWordVkCodes.Count > 0)
                        _currentWordVkCodes.RemoveAt(_currentWordVkCodes.Count - 1);
                }
                return false;
            }

            IntPtr hWnd = Win32.GetForegroundWindow();
            if (hWnd == IntPtr.Zero) return false;

            uint processId;
            uint threadId = Win32.GetWindowThreadProcessId(hWnd, out processId);
            
            // Получаем раскладку АКТИВНОГО потока
            IntPtr hKl = Win32.GetKeyboardLayout(threadId);
            ushort langId = (ushort)((long)hKl & 0xFFFF);

            // Получаем состояние клавиатуры для правильного регистра
            byte[] keyState = new byte[256];
            Win32.GetKeyboardState(keyState);

            StringBuilder sb = new StringBuilder(5);
            int rc = Win32.ToUnicodeEx((uint)vkCode, (uint)scanCode, keyState, sb, sb.Capacity, 0, hKl);

            if (rc > 0)
            {
                char c = sb[0];
                
                // Если разделитель - сбрасываем слово
                bool isSeparator = char.IsWhiteSpace(c) || char.IsDigit(c);
                
                // Punctuation that acts as letters in other layouts should NOT be separators
                // EN: [ ] ; ' , . /
                // RU: . ,
                if (!isSeparator && char.IsPunctuation(c))
                {
                    // If it's one of the "letter-like" punctuation marks, keep it
                    if (c == '[' || c == ']' || c == ';' || c == '\'' || c == ',' || c == '.' || c == '/' || c == '`')
                    {
                        isSeparator = false;
                    }
                    else
                    {
                        // Other punctuation (e.g. !) might be a separator or just ignored
                        // Let's treat them as separators to be safe and clear buffer
                        // isSeparator = true; 
                        // Actually, better to just NOT add them to word, but NOT clear.
                        // But for now, let's stick to clearing on explicit separators.
                    }
                }

                if (isSeparator)
                {
                    ClearWord();
                    return false;
                }

                bool isRuLayout = (langId & 0x3FF) == 0x19; 
                bool isEnLayout = (langId & 0x3FF) == 0x09;

                // Эвристика 1: Недопустимые символы в начале слова (RU)
                if (isRuLayout && _currentWord.Length == 0)
                {
                    if (c == 'ь' || c == 'ъ' || c == 'ы' || c == 'Ь' || c == 'Ъ' || c == 'Ы')
                    {
                        Log($"Heuristic: Illegal start char '{c}' in RU. Switching to EN.");
                        
                        // Добавляем текущий символ в буфер перед переключением, чтобы он тоже был удален и перепечатан
                        _currentWordVkCodes.Add(vkCode);
                        
                        SwitchToLanguage(0x09);
                        RewriteLastWord(true); // Suppress current key
                        ClearWord();
                        return true;
                    }
                }

                // Эвристика 2: Подозрительные символы внутри слова (EN)
                if (isEnLayout)
                {
                    // Если слово начинается с апострофа - это скорее всего 'э' в RU (например "это" -> "'nj")
                    if (_currentWord.Length == 0 && c == '\'')
                    {
                        Log($"Heuristic: Word starts with apostrophe in EN. Switching to RU.");
                        _currentWordVkCodes.Add(vkCode);
                        SwitchToLanguage(0x19);
                        RewriteLastWord(true);
                        ClearWord();
                        return true;
                    }

                    if (_currentWord.Length > 0)
                    {
                        if (c == '[' || c == ']' || c == ';' || c == '\'')
                        {
                            Log($"Heuristic: Suspicious char '{c}' inside EN word. Switching to RU.");
                            
                            _currentWordVkCodes.Add(vkCode);
                            
                            SwitchToLanguage(0x19);
                            RewriteLastWord(true); // Suppress current key
                            ClearWord();
                            return true;
                        }
                    }
                }

                // Add to buffer if it's a letter OR one of our special punctuation marks
                bool isPotentialLetter = char.IsLetter(c) || 
                                         (c == '[' || c == ']' || c == ';' || c == '\'' || c == ',' || c == '.' || c == '/' || c == '`');

                if (isPotentialLetter)
                {
                    _currentWord.Append(c);
                    _currentWordVkCodes.Add(vkCode);
                }

                // Эвристика: короткие русские слова, набранные в EN раскладке (<=4 символов)
                if (isEnLayout && _currentWord.Length >= 2 && _currentWord.Length <= 4)
                {
                    string enWord = _currentWord.ToString();
                    string ruMapped = MapEnToRu(enWord);
                    string ruNormalized = ruMapped.Replace(" ", string.Empty);
                    if (_ruShortWhitelist.Contains(ruNormalized))
                    {
                        Log($"Heuristic: Short RU word from EN mapping '{enWord}' -> '{ruMapped}'. Switching to RU.");
                        SwitchToLanguage(0x19);
                        RewriteLastWord(true);
                        ClearWord();
                        return true;
                    }
                    bool allMappable = true;
                    foreach (char ch in enWord) { if (!_enToRu.ContainsKey(ch)) { allMappable = false; break; } }
                    if (allMappable)
                    {
                        bool ruHasVowel = false;
                        foreach (char wc in ruMapped) { if (_ruVowels.Contains(wc)) { ruHasVowel = true; break; } }
                        if (ruHasVowel)
                        {
                            Log($"Heuristic: EN→RU mapped word '{enWord}' has RU vowel(s). Switching to RU.");
                            SwitchToLanguage(0x19);
                            RewriteLastWord(true);
                            ClearWord();
                            return true;
                        }
                    }
                }

                if (isEnLayout && _currentWord.Length >= 5 && _currentWord.Length <= 12)
                {
                    string enWord = _currentWord.ToString();
                    bool allLetters = true;
                    foreach (char ch in enWord) { if (!char.IsLetter(ch)) { allLetters = false; break; } }
                    if (allLetters)
                    {
                        bool allMappable = true;
                        foreach (char ch in enWord) { if (!_enToRu.ContainsKey(ch)) { allMappable = false; break; } }
                        if (allMappable)
                        {
                            string ruMapped = MapEnToRu(enWord);
                            int enVowelCount = 0;
                            foreach (char wc in enWord) { if (_enVowels.Contains(wc)) enVowelCount++; }
                            bool ruHasVowel = false;
                            foreach (char wc in ruMapped) { if (_ruVowels.Contains(wc)) { ruHasVowel = true; break; } }
                            bool ruHasHardChars = false;
                            foreach (char wc in ruMapped) { if (wc=='ы'||wc=='ш'||wc=='щ'||wc=='ж'||wc=='ю'||wc=='я'||wc=='й'||wc=='э'||wc=='ь'||wc=='ъ'||wc=='ё'||wc=='Ы'||wc=='Ш'||wc=='Щ'||wc=='Ж'||wc=='Ю'||wc=='Я'||wc=='Й'||wc=='Э'||wc=='Ь'||wc=='Ъ'||wc=='Ё') { ruHasHardChars = true; break; } }
                            if (ruHasVowel && ruHasHardChars && enVowelCount <= 1)
                            {
                                Log($"Heuristic: EN→RU mapped long word '{enWord}' -> '{ruMapped}'. Switching to RU.");
                                SwitchToLanguage(0x19);
                                RewriteLastWord(true);
                                ClearWord();
                                return true;
                            }
                        }
                    }
                }

                // Эвристика 3: Слово без гласных (для "ghbdtn" -> "привет", "rfr" -> "как")
                if (_currentWord.Length >= 3)
                {
                    string word = _currentWord.ToString();
                    
                    // Игнорируем аббревиатуры (все заглавные)
                    bool isAllUpper = true;
                    foreach (char wc in word) if (char.IsLower(wc)) { isAllUpper = false; break; }
                    
                    if (!isAllUpper)
                    {
                        if (isEnLayout)
                        {
                            if (_settings.Exceptions.Contains(word)) return true; // Valid exception

                            bool hasVowels = false;
                            foreach (char wc in word)
                            {
                                if (_enVowels.Contains(wc)) { hasVowels = true; break; }
                            }

                            if (!hasVowels)
                            {
                                Log($"Heuristic: Word '{word}' (>=3 chars) has no EN vowels. Switching to RU.");
                                SwitchToLanguage(0x19);
                                RewriteLastWord(true); // Suppress current key
                                ClearWord();
                                return true;
                            }
                        }
                        
                        if (isRuLayout)
                        {
                            bool hasVowels = false;
                            foreach (char wc in word)
                            {
                                if (_ruVowels.Contains(wc)) { hasVowels = true; break; }
                            }

                            if (!hasVowels)
                            {
                                Log($"Heuristic: Word '{word}' (>=3 chars) has no RU vowels. Switching to EN.");
                                SwitchToLanguage(0x09);
                                RewriteLastWord(true); // Suppress current key
                                ClearWord();
                                return true;
                            }
                        }
                    }
                }

                // Старая проверка диапазонов (менее приоритетна, так как может ложно срабатывать)
                // Но если сработала, тоже переписываем
                bool isLatin = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
                bool isCyrillic = (c >= 0x0400 && c <= 0x04FF);

                if (isLatin && isRuLayout)
                {
                    Log($"Mismatch: Latin char '{c}' in RU layout. Switching to EN.");
                    // Тут аккуратно, может быть просто одна буква.
                    // Но если мы уверены, то переписываем.
                    // Для одиночных букв переписывание тоже работает.
                    // Но нужно добавить текущий код, если он еще не добавлен (он добавляется выше если IsLetter)
                    
                    SwitchToLanguage(0x09);
                    RewriteLastWord(true); // Suppress current key
                    ClearWord();
                    return true;
                }
                else if (isCyrillic && isEnLayout)
                {
                    Log($"Mismatch: Cyrillic char '{c}' in EN layout. Switching to RU.");
                    SwitchToLanguage(0x19);
                    RewriteLastWord(true); // Suppress current key
                    ClearWord();
                    return true;
                }
            }
            return false;
        }

        public void ManualSwitch()
        {
            Log("Manual switch requested.");
            IntPtr hWnd = Win32.GetForegroundWindow();
            if (hWnd == IntPtr.Zero) return;

            uint processId;
            uint threadId = Win32.GetWindowThreadProcessId(hWnd, out processId);
            IntPtr hKl = Win32.GetKeyboardLayout(threadId);
            ushort langId = (ushort)((long)hKl & 0xFFFF);

            // Toggle between RU (0x19) and EN (0x09)
            bool isRu = (langId & 0x3FF) == 0x19;
            ushort targetLang = isRu ? (ushort)0x09 : (ushort)0x19;

            SwitchToLanguage(targetLang);
            RewriteLastWord(false); // Don't suppress current key (it was the hotkey)
            ClearWord();
        }

        private void ClearWord()
        {
            _currentWord.Clear();
            _currentWordVkCodes.Clear();
        }

        private void RewriteLastWord(bool suppressCurrent)
        {
            if (_currentWordVkCodes.Count == 0) return;

            int count = _currentWordVkCodes.Count;
            // If we suppress the current key, the app hasn't received it yet.
            // So we need to backspace (count - 1) times.
            int backspaces = suppressCurrent ? count - 1 : count;
            
            Log($"Rewriting {count} chars (suppress={suppressCurrent}, BS={backspaces})...");

            List<Win32.INPUT> inputs = new List<Win32.INPUT>();

            // 1. Backspaces to delete old chars
            for (int i = 0; i < backspaces; i++)
            {
                Win32.INPUT down = new Win32.INPUT { type = Win32.INPUT_KEYBOARD };
                down.U.ki.wVk = Win32.VK_BACK;
                inputs.Add(down);

                Win32.INPUT up = new Win32.INPUT { type = Win32.INPUT_KEYBOARD };
                up.U.ki.wVk = Win32.VK_BACK;
                up.U.ki.dwFlags = Win32.KEYEVENTF_KEYUP;
                inputs.Add(up);
            }

            // 2. Re-type chars with new layout
            foreach (int vk in _currentWordVkCodes)
            {
                Win32.INPUT down = new Win32.INPUT { type = Win32.INPUT_KEYBOARD };
                down.U.ki.wVk = (ushort)vk;
                inputs.Add(down);

                Win32.INPUT up = new Win32.INPUT { type = Win32.INPUT_KEYBOARD };
                up.U.ki.wVk = (ushort)vk;
                up.U.ki.dwFlags = Win32.KEYEVENTF_KEYUP;
                inputs.Add(up);
            }

            Win32.SendInput((uint)inputs.Count, inputs.ToArray(), Win32.INPUT.Size);
        }

        private void SwitchToLanguage(ushort targetPrimaryLangId)
        {
            int count = GetKeyboardLayoutList(0, null);
            if (count > 0)
            {
                IntPtr[] hkls = new IntPtr[count];
                GetKeyboardLayoutList(count, hkls);

                foreach (var hkl in hkls)
                {
                    ushort lid = (ushort)((long)hkl & 0xFFFF);
                    if ((lid & 0x3FF) == targetPrimaryLangId)
                    {
                        ActivateLayout(hkl);
                        return;
                    }
                }
            }
        }

        private void ActivateLayout(IntPtr hkl)
        {
            short langId = (short)((long)hkl & 0xFFFF);

            if (_profileMgr != null)
            {
                Guid clsid = Guid.Empty;
                Guid profile = Guid.Empty;
                try
                {
                    _profileMgr.ActivateProfile(Win32.TF_PROFILETYPE_KEYBOARDLAYOUT, langId, ref clsid, ref profile, hkl, Win32.TF_IPPMF_FORSESSION);
                    Log($"Switched to {langId:X4} via TSF");
                    if (_settings.PlaySound) Win32.MessageBeep(0);
                    return;
                }
                catch (Exception ex)
                {
                    Log($"TSF Error switching to {langId:X4}: {ex.Message}. Trying fallback...");
                }
            }

            // Fallback
            IntPtr hWnd = Win32.GetForegroundWindow();
            if (hWnd != IntPtr.Zero)
            {
                Win32.PostMessage(hWnd, Win32.WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, hkl);
                Log($"Switched to {langId:X4} via PostMessage");
                if (_settings.PlaySound) Win32.MessageBeep(0);
            }
        }

        private void Log(string msg)
        {
            try
            {
                FileInfo fi = new FileInfo(_logPath);
                if (fi.Exists && fi.Length > 1024 * 1024)
                {
                    File.Delete(_logPath);
                }
                File.AppendAllText(_logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {msg}{Environment.NewLine}");
            }
            catch { }
        }
        private string MapEnToRu(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char ch in s)
            {
                if (_enToRu.TryGetValue(ch, out char ru)) sb.Append(ru);
                else sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}
