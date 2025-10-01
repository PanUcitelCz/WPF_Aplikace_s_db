using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPF_Aplikace_s_db.Data;
using Microsoft.VisualBasic;
using System.Linq;

namespace WPF_Aplikace_s_db
{
    public partial class MainWindow : Window
    {
        private readonly StudentContext _db = new StudentContext();
        private readonly ObservableCollection<Student> _students = new ObservableCollection<Student>();
        private ICollectionView _studentsView;

        public MainWindow()
        {
            InitializeComponent();

            _db.EnsureCreatedAndSeed();

            foreach (var s in _db.Students.OrderBy(x => x.Id).ToList())
                _students.Add(s);

            _studentsView = CollectionViewSource.GetDefaultView(_students);
            _studentsView.SortDescriptions.Clear();
            _studentsView.SortDescriptions.Add(new SortDescription(nameof(Student.Id), ListSortDirection.Ascending));

            StudentsGrid.ItemsSource = _studentsView;
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _db.Dispose();
            base.OnClosed(e);
        }

        private void BtnAddStudent_Click(object sender, RoutedEventArgs e)
        {
            string firstName = (TxtFirstName.Text ?? string.Empty).Trim();
            string lastName = (TxtLastName.Text ?? string.Empty).Trim();
            string email = (TxtEmail.Text ?? string.Empty).Trim();
            int year = 1;
            int.TryParse((TxtYear.Text ?? string.Empty).Trim(), out year);

            var s = new Student
            {
                FirstName = firstName,
                LastName = lastName,
                Year = year,
                Email = email,
                CreatedAt = System.DateTime.UtcNow
            };

            _db.Students.Add(s);
            _db.SaveChanges();

            _students.Add(s);
            StudentsGrid.SelectedItem = s;
            StudentsGrid.ScrollIntoView(s);

            TxtFirstName.Text = TxtLastName.Text = TxtEmail.Text = TxtYear.Text = string.Empty;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StudentsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                StudentsGrid.CommitEdit(DataGridEditingUnit.Row, true);

                if (!_db.ChangeTracker.HasChanges())
                {
                    MessageBox.Show("Žádné změny k uložení.", "Informace",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _db.SaveChanges();
                MessageBox.Show("Změny byly uloženy.", "Hotovo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Nepodařilo se uložit změny.\n\n" + ex, "Chyba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = StudentsGrid.SelectedItem as Student;
            if (selected == null)
            {
                MessageBox.Show("Nejprve vyberte studenta v tabulce.", "Upozornění",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var answer = MessageBox.Show("Opravdu smazat vybraného studenta?",
                "Potvrzení", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
                return;

            _db.Students.Remove(selected);
            try
            {
                _db.SaveChanges();
                _students.Remove(selected);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Smazání se nepodařilo.\n\n" + ex, "Chyba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
