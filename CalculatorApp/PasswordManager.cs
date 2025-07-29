using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CalculatorApp
{
    public static class PasswordManager
    {
        // ВСТРОЕННЫЙ ПАРОЛЬ ПО УМОЛЧАНИЮ - ИЗМЕНИТЕ ЭТО ЗНАЧЕНИЕ ДЛЯ СОЗДАНИЯ КАСТОМНОЙ ВЕРСИИ
        private const string DEFAULT_EMBEDDED_PASSWORD = "admin123"; // ← Измените этот пароль

        // Сохраняем пароль в папку данных пользователя (AppData)
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DiKom Calculator"
        );

        private static readonly string PasswordFile = Path.Combine(AppDataFolder, ".Fi8Hhc80jbT2c9c1");
        private static string _currentPasswordHash;

        public static void Initialize()
        {
            try
            {
                // Создаем папку данных приложения если её нет
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                // Загружаем хеш пароля из скрытого файла
                if (File.Exists(PasswordFile))
                {
                    var savedHash = File.ReadAllText(PasswordFile).Trim();
                    if (!string.IsNullOrEmpty(savedHash))
                    {
                        _currentPasswordHash = savedHash;
                    }
                    else
                    {
                        // Файл пуст, создаем хеш по умолчанию
                        _currentPasswordHash = HashPassword(DEFAULT_EMBEDDED_PASSWORD);
                        SavePasswordHash();
                    }
                }
                else
                {
                    // При первом запуске сохраняем хеш пароля по умолчанию
                    _currentPasswordHash = HashPassword(DEFAULT_EMBEDDED_PASSWORD);
                    SavePasswordHash();
                }
            }
            catch
            {
                // В случае ошибки используем пароль по умолчанию
                _currentPasswordHash = HashPassword(DEFAULT_EMBEDDED_PASSWORD);
            }
        }

        public static bool ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            var hashToCheck = HashPassword(password);
            return hashToCheck == _currentPasswordHash;
        }

        public static bool ChangePassword(string currentPassword, string newPassword)
        {
            if (!ValidatePassword(currentPassword))
                return false;

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 3)
                return false;

            try
            {
                // Обновляем хеш в памяти
                _currentPasswordHash = HashPassword(newPassword);

                // Сохраняем зашифрованный хеш в файл
                SavePasswordHash();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void ResetToDefault()
        {
            try
            {
                // Сбрасываем к хешу пароля по умолчанию
                _currentPasswordHash = HashPassword(DEFAULT_EMBEDDED_PASSWORD);

                // Сохраняем зашифрованный хеш в файл
                SavePasswordHash();
            }
            catch
            {
                // В случае ошибки используем пароль по умолчанию только в памяти
                _currentPasswordHash = HashPassword(DEFAULT_EMBEDDED_PASSWORD);
            }
        }

        private static void SavePasswordHash()
        {
            try
            {
                // Создаем папку данных приложения если её нет
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                // Сохраняем только зашифрованный хеш, а не сам пароль
                File.WriteAllText(PasswordFile, _currentPasswordHash);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения пароля: {ex.Message}\n\nПуть: {PasswordFile}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "CalculatorAppSalt"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public static string GetDefaultPassword()
        {
            return DEFAULT_EMBEDDED_PASSWORD;
        }

        public static string GetCurrentPassword()
        {
            // Мы не можем вернуть расшифрованный пароль из хеша,
            // поэтому возвращаем пароль по умолчанию для справки
            return DEFAULT_EMBEDDED_PASSWORD;
        }
    }
}
