using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace WpfApp4
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<User> users;
        private List<User> allUsers;
        private HashSet<User> selectedUsers;
        private const string DataFilePath = "users.json";
        private const string ConfigFilePath = "config.json";

        public MainWindow()
        {
            InitializeComponent();
            LoadUsers();
            selectedUsers = new HashSet<User>();
        }

        private async void LoadUsers()
        {
            try
            {
                LoadingProgressBar.Visibility = Visibility.Visible; // Показываем ProgressBar

                string apiUrl = GetApiUrlFromConfig();

                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    allUsers = JsonConvert.DeserializeObject<List<User>>(responseBody);

                    // Сохраняем данные в файл
                    File.WriteAllText(DataFilePath, responseBody);
                }

                users = new ObservableCollection<User>(allUsers);

                // Отображение данных в интерфейсе
                UsersListBox.ItemsSource = users;
                PrintAllUsersListBox.ItemsSource = users;

                // Обновление времени последнего обновления
                LastUpdatedTextBlock.Text = $"Последнее обновление: {DateTime.Now}";
                LastUpdatedTextBlockPrintAll.Text = $"Последнее обновление: {DateTime.Now}";

                // Обновление общего количества посетителей
                TotalUsersTextBlock.Text = $"Всего регистраций: {allUsers.Count}";
                TotalUsersTextBlockPrintAll.Text = $"Всего регистраций: {allUsers.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                LoadingProgressBar.Visibility = Visibility.Collapsed; // Скрываем ProgressBar
            }
        }

        private string GetApiUrlFromConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                dynamic config = JsonConvert.DeserializeObject(json);
                return config.apiUrl;
            }
            else
            {
                throw new FileNotFoundException("Конфигурационный файл config.json не найден.", ConfigFilePath);
            }
        }

        private void RefreshDataButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower();
            var filteredUsers = allUsers.Where(user =>
                user.first_name.ToLower().Contains(searchText) ||
                user.last_name.ToLower().Contains(searchText.ToLower()) ||
                user.Email.ToLower().Contains(searchText)).ToList();

            users.Clear();
            foreach (var user in filteredUsers)
            {
                users.Add(user);
            }
        }

        private void PrintAllSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = PrintAllSearchTextBox.Text.ToLower();

            // Сохраняем текущие выделенные пользователи
            var currentlySelectedUsers = new HashSet<User>(PrintAllUsersListBox.SelectedItems.Cast<User>());

            // Фильтрация пользователей
            var filteredUsers = allUsers.Where(user =>
                user.first_name.ToLower().Contains(searchText) ||
                user.last_name.ToLower().Contains(searchText) ||
                user.Email.ToLower().Contains(searchText)).ToList();

            // Обновляем источник данных для ListBox
            users.Clear();
            foreach (var user in filteredUsers)
            {
                users.Add(user);
            }

            // Восстанавливаем выделение
            PrintAllUsersListBox.SelectedItems.Clear(); // Сначала очищаем выделение
            foreach (var user in filteredUsers)
            {
                // Если пользователь был ранее выбран, добавляем его обратно в выделенные элементы
                if (currentlySelectedUsers.Contains(user))
                {
                    PrintAllUsersListBox.SelectedItems.Add(user);
                }
            }

            // Обновляем счетчик выбранных пользователей
            SelectedUsersCount.Text = $"Выбрано: {PrintAllUsersListBox.SelectedItems.Count}";
        }

        private void UsersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersListBox.SelectedItem != null)
            {
                User selectedUser = (User)UsersListBox.SelectedItem;
                SelectedUserInfo.Text = $"Имя: {selectedUser.first_name}\nФамилия: {selectedUser.last_name}\nEmail: {selectedUser.Email}";

            }
            else
            {
                SelectedUserInfo.Text = "Выберите пользователя, чтобы увидеть больше информации.";
            }
        }

        private void PrintAllUsersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обновляем выбор пользователей
            selectedUsers.Clear();
            foreach (User user in PrintAllUsersListBox.SelectedItems)
            {
                selectedUsers.Add(user);
            }

            SelectedUsersCount.Text = $"Выбрано: {selectedUsers.Count}";
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersListBox.SelectedItems.Count > 0)
            {
                List<User> selectedUsers = UsersListBox.SelectedItems.Cast<User>().ToList();
                PrintUsers(selectedUsers);
            }
            else
            {
                MessageBox.Show("Please select at least one user to print information.");
            }
        }

        private void PrintSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUsers.Count > 0)
            {
                PrintUsers(selectedUsers.ToList());
            }
            else
            {
                MessageBox.Show("Please select at least one user to print information.");
            }
        }

        private void PrintAllButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем количество пользователей для печати
            int userCount = allUsers.Count;

            // Запрос подтверждения у пользователя
            MessageBoxResult result = MessageBox.Show(
                $"Вы точно хотите напечатать все {userCount} бейджей?",
                "Подтверждение печати",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            // Если пользователь подтвердил, выполняем печать
            if (result == MessageBoxResult.Yes)
            {
                PrintUsers(allUsers);
            }
        }

        private void UnselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            PrintAllUsersListBox.UnselectAll();
            selectedUsers.Clear();
            SelectedUsersCount.Text = "Выбрано: 0";
        }

        private void PrintUsers(List<User> users)
        {
            StringBuilder printCommands = new StringBuilder();

            foreach (var user in users)
            {
                printCommands.Append($@"
^Q50,7
^XA
^MMT
^PW640
^LL400
^LT0
^LH0,0
^LS0

^CI28
^FO10,60^A0N,50,50^FB620,3,0,C,0^FD{user.first_name.ToUpper()} \& ^FS
^FO10,140^A0N,50,50^FB620,3,0,C,0^FD{user.last_name.ToUpper()} \& ^FS
^FO120,250^BY2
^BCN,100,Y,N,N,N^FD{user.Code.ToUpper()}^FS

^XZ
");
            }

            // Отправляем команды на принтер
            RawPrinterHelper.SendStringToPrinter("Godex GE300", printCommands.ToString());
        }

    }
}
