# WPF (.NET 9) + EF Core: Students CRUD — výukový projekt

Tento **výukový README** krok‑za‑krokem ukazuje, jak ve Visual Studiu 2022 vytvořit **WPF aplikaci** s databází přes **Entity Framework Core (SQL Server LocalDB)**. Projekt umí:

1) **Vypsat** tabulku studentů do `DataGridu`  
2) **Přidávat** nové záznamy z formuláře (bez validace)  
3) **Upravovat** a **mazat** záznamy přímo v mřížce + **Uložit** změny do DB  
4) **Zamezit chybnému vstupu** u „Ročník“ (výběr jen 1–6 přes rozbalovací seznam)

> Vše je **klikací** ve VS 2022 (žádný terminál).

---

## 0) Příprava projektu (klikací postup)

### 0.1 Vytvoření projektu

1. Otevři **Visual Studio 2022**.  
2. **Create a new project** → vyber **WPF Aplication** (.NET) → **Next**.  
3. **Project name** zadej např. `WPF_Aplikace_s_db`.  
4. **Framework** nastav na **.NET 9.0** → **Create**.

### 0.2 Instalace NuGet balíčků

V **Solution Exploreru** klikni pravým na projekt → **Manage NuGet Packages…** → záložka **Browse** a nainstaluj postupně:

- **Microsoft.EntityFrameworkCore.SqlServer**  
  Poskytovatel EF Core pro SQL Server (včetně **LocalDB**). Díky němu aplikace mluví s SQL Serverem.

- **Microsoft.EntityFrameworkCore.Tools**  
  Design‑time nástroje EF (pomáhají s migracemi a návrhem). I když tu používáme `EnsureCreated`, hodí se do budoucna.

- **PropertyChanged.Fody** *(nainstaluje i balíček **Fody**)*  
  Automaticky doplní `INotifyPropertyChanged` do tříd označených atributem `[AddINotifyPropertyChangedInterface]`. Změny ve vlastnostech se pak hned promítnou v UI bez ručního psaní notifikací.

### 0.3 Nastavení Fody (pokud si o to projekt řekne)
Pokud si Fody vyžádá konfigurační soubor, přidej do projektu nový soubor **`FodyWeavers.xml`** s obsahem:

```xml
<Weavers>
  <PropertyChanged />
</Weavers>
```

---

# Učební postup po úkolech

Níže každou novou část vždy ukazuji **nejdřív bez komentářů** a hned **poté s komentáři**. Kód vychází z hotového řešení ve zdrojích (Student, StudentContext, MainWindow).

> **Databáze**: používáme SQL Server **LocalDB** (automaticky vytvoří databázi při prvním spuštění). Pokud později změníš strukturu tabulek a budeš chtít čistý start, ve VS otevři **SQL Server Object Explorer** a databázi smaž, nebo v connection stringu změň `Initial Catalog` na jiné jméno.

---

## Úkol 1 — Vypsat data ze StudentContextu do DataGridu

### 1A) Model `Student`

**Bez komentářů:**

```csharp
using PropertyChanged;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPF_Aplikace_s_db.Data
{
    [AddINotifyPropertyChangedInterface]
    public class Student
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string LastName { get; set; } = string.Empty;

        [Range(1, 6)]
        public int Year { get; set; }

        [StringLength(50)]
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

**S komentáři:**

```csharp
using PropertyChanged;                             // Weaver pro automatické INotifyPropertyChanged
using System;
using System.ComponentModel.DataAnnotations;       // DataAnnotations (Required, StringLength, Range…)
using System.ComponentModel.DataAnnotations.Schema;

namespace WPF_Aplikace_s_db.Data
{
    // Atribut z PropertyChanged.Fody – automaticky přidá INotifyPropertyChanged pro všechny vlastnosti
    [AddINotifyPropertyChangedInterface]
    public class Student
    {
        [Key]                                       // Primární klíč
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Identity = DB generuje Id
        public int Id { get; set; }

        [Required]                                   // Povinné pole
        [StringLength(30)]                           // Maximálně 30 znaků
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string LastName { get; set; } = string.Empty;

        [Range(1, 6)]                                // Povolený rozsah 1–6
        public int Year { get; set; }

        [StringLength(50)]
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Datum vytvoření (UTC)
    }
}
```

### 1B) Databázový kontext `StudentContext` + seed dat

**Bez komentářů:**

```csharp
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace WPF_Aplikace_s_db.Data
{
    public class StudentContext : DbContext
    {
        public DbSet<Student> Students => Set<Student>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    "Data Source=(localdb)\\MSSQLLocalDB;" +
                    "Initial Catalog=StudentDbDemo1;" +
                    "Integrated Security=True;" +
                    "TrustServerCertificate=True");
            }
        }

        public void EnsureCreatedAndSeed()
        {
            Database.EnsureCreated();
            SeedIfEmpty();
        }

        public void SeedIfEmpty()
        {
            if (!Students.Any())
            {
                var initial = new List<Student>
                {
                    new Student { FirstName="Jan",   LastName="Novák",    Year=1, Email="jan.novak@example.com" },
                    new Student { FirstName="Petr",  LastName="Svoboda",  Year=2, Email="petr.svoboda@example.com" },
                    new Student { FirstName="Karel", LastName="Černý",    Year=3, Email="karel.cerny@example.com" },
                    new Student { FirstName="Lucie", LastName="Malá",     Year=1, Email="lucie.mala@example.com" },
                    new Student { FirstName="Eva",   LastName="Bílá",     Year=2, Email="eva.bila@example.com" },
                    new Student { FirstName="Adam",  LastName="Zelený",   Year=3, Email="adam.zeleny@example.com" },
                    new Student { FirstName="Tomáš", LastName="Dvořák",   Year=1, Email="tomas.dvorak@example.com" },
                    new Student { FirstName="Marie", LastName="Veselá",   Year=2, Email="marie.vesela@example.com" },
                    new Student { FirstName="Jana",  LastName="Horáková", Year=3, Email="jana.horakova@example.com" },
                    new Student { FirstName="Filip", LastName="Král",     Year=1, Email="filip.kral@example.com" }
                };
                Students.AddRange(initial);
                SaveChanges();
            }
        }
    }
}
```

**S komentáři:**

```csharp
using Microsoft.EntityFrameworkCore;        // EF Core – DbContext, DbSet, UseSqlServer
using System.Collections.Generic;
using System.Linq;

namespace WPF_Aplikace_s_db.Data
{
    public class StudentContext : DbContext
    {
        // DbSet = kolekce entit Student, EF z ní vytvoří tabulku Students
        public DbSet<Student> Students => Set<Student>();

        // Nastavení připojení – tady SQL Server LocalDB
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    "Data Source=(localdb)\\MSSQLLocalDB;" +   // lokální SQL Server (LocalDB)
                    "Initial Catalog=StudentDbDemo1;" +        // název databáze
                    "Integrated Security=True;" +              // integrované přihlášení Windows
                    "TrustServerCertificate=True");            // pro LocalDB OK
            }
        }

        // Vytvoří DB (pokud neexistuje) a naplní základními daty
        public void EnsureCreatedAndSeed()
        {
            Database.EnsureCreated();
            SeedIfEmpty();
        }

        // První naplnění tabulky Students – jen když je prázdná
        public void SeedIfEmpty()
        {
            if (!Students.Any())
            {
                var initial = new List<Student>
                {
                    new Student { FirstName="Jan",   LastName="Novák",    Year=1, Email="jan.novak@example.com" },
                    new Student { FirstName="Petr",  LastName="Svoboda",  Year=2, Email="petr.svoboda@example.com" },
                    new Student { FirstName="Karel", LastName="Černý",    Year=3, Email="karel.cerny@example.com" },
                    new Student { FirstName="Lucie", LastName="Malá",     Year=1, Email="lucie.mala@example.com" },
                    new Student { FirstName="Eva",   LastName="Bílá",     Year=2, Email="eva.bila@example.com" },
                    new Student { FirstName="Adam",  LastName="Zelený",   Year=3, Email="adam.zeleny@example.com" },
                    new Student { FirstName="Tomáš", LastName="Dvořák",   Year=1, Email="tomas.dvorak@example.com" },
                    new Student { FirstName="Marie", LastName="Veselá",   Year=2, Email="marie.vesela@example.com" },
                    new Student { FirstName="Jana",  LastName="Horáková", Year=3, Email="jana.horakova@example.com" },
                    new Student { FirstName="Filip", LastName="Král",     Year=1, Email="filip.kral@example.com" }
                };
                Students.AddRange(initial);
                SaveChanges();
            }
        }
    }
}
```

### 1C) `MainWindow.xaml` – DataGrid se sloupci

**Bez komentářů:**

```xml
<Window x:Class="WPF_Aplikace_s_db.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Students" Height="450" Width="800">
    <Grid Margin="12">
        <DataGrid x:Name="StudentsGrid"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  IsReadOnly="False">
            <DataGrid.Columns>
                <DataGridTextColumn Header="ID"        Binding="{Binding Id}"        Width="70"/>
                <DataGridTextColumn Header="Jméno"     Binding="{Binding FirstName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
                <DataGridTextColumn Header="Příjmení"  Binding="{Binding LastName,  Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
                <DataGridTextColumn Header="Ročník"    Binding="{Binding Year,      Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="90"/>
                <DataGridTextColumn Header="E‑mail"    Binding="{Binding Email,     Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="2*"/>
                <DataGridTextColumn Header="Vytvořeno" Binding="{Binding CreatedAt, StringFormat={}{0:yyyy-MM-dd HH:mm:ss}}" Width="180"/>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</Window>
```

**S komentáři:**

```xml
<Window x:Class="WPF_Aplikace_s_db.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Students" Height="450" Width="800">
    <Grid Margin="12">
        <!-- DataGrid zobrazí kolekci Studentů nastavenou v code‑behind do ItemsSource -->
        <DataGrid x:Name="StudentsGrid"
                  AutoGenerateColumns="False"   <!-- sloupce definujeme ručně -->
                  CanUserAddRows="False"        <!-- řádek pro přidání skryjeme -->
                  IsReadOnly="False">           <!-- povolíme editaci buněk -->
            <DataGrid.Columns>
                <DataGridTextColumn Header="ID"        Binding="{Binding Id}"        Width="70"/>
                <DataGridTextColumn Header="Jméno"     Binding="{Binding FirstName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
                <DataGridTextColumn Header="Příjmení"  Binding="{Binding LastName,  Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
                <DataGridTextColumn Header="Ročník"    Binding="{Binding Year,      Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="90"/>
                <DataGridTextColumn Header="E‑mail"    Binding="{Binding Email,     Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="2*"/>
                <DataGridTextColumn Header="Vytvořeno" Binding="{Binding CreatedAt, StringFormat={}{0:yyyy-MM-dd HH:mm:ss}}" Width="180"/>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</Window>
```

### 1D) `MainWindow.xaml.cs` – načtení dat do mřížky

**Bez komentářů:**

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using WPF_Aplikace_s_db.Data;

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
    }
}
```

**S komentáři:**

```csharp
using System.Collections.ObjectModel;         // ObservableCollection pro bindování do DataGridu
using System.ComponentModel;                 // ICollectionView a třídění
using System.Linq;                           // LINQ (OrderBy, ToList)
using System.Windows;
using System.Windows.Data;
using WPF_Aplikace_s_db.Data;

namespace WPF_Aplikace_s_db
{
    public partial class MainWindow : Window
    {
        private readonly StudentContext _db = new StudentContext(); // EF DbContext
        private readonly ObservableCollection<Student> _students = new ObservableCollection<Student>(); // data pro grid
        private ICollectionView _studentsView; // obálka nad kolekcí – umožní třídění, filtrování…

        public MainWindow()
        {
            InitializeComponent();

            _db.EnsureCreatedAndSeed(); // vytvoří DB a naplní vzorovými daty (pokud je prázdná)

            // Načti studenty z DB a vlož do ObservableCollection (kvůli okamžitému updatu UI)
            foreach (var s in _db.Students.OrderBy(x => x.Id).ToList())
                _students.Add(s);

            // Připrav zobrazení a setřiď podle Id
            _studentsView = CollectionViewSource.GetDefaultView(_students);
            _studentsView.SortDescriptions.Clear();
            _studentsView.SortDescriptions.Add(new SortDescription(nameof(Student.Id), ListSortDirection.Ascending));

            // Napoj DataGrid
            StudentsGrid.ItemsSource = _studentsView;
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _db.Dispose();  // zavři DB připojení
            base.OnClosed(e);
        }
    }
}
```

---

## Úkol 2 — Přidávání záznamu (bez validace)

### 2A) `MainWindow.xaml` – jednoduchý formulář dole + tlačítko

**Bez komentářů:**

```xml
<Grid Margin="12">
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
    <RowDefinition Height="Auto"/>
  </Grid.RowDefinitions>

  <DataGrid x:Name="StudentsGrid"
            Grid.Row="0"
            AutoGenerateColumns="False"
            CanUserAddRows="False"
            IsReadOnly="False">
    <DataGrid.Columns>
      <DataGridTextColumn Header="ID"        Binding="{Binding Id}"        Width="70"/>
      <DataGridTextColumn Header="Jméno"     Binding="{Binding FirstName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
      <DataGridTextColumn Header="Příjmení"  Binding="{Binding LastName,  Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
      <DataGridTextColumn Header="Ročník"    Binding="{Binding Year,      Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="90"/>
      <DataGridTextColumn Header="E‑mail"    Binding="{Binding Email,     Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="2*"/>
      <DataGridTextColumn Header="Vytvořeno" Binding="{Binding CreatedAt, StringFormat={}{0:yyyy-MM-dd HH:mm:ss}}" Width="180"/>
    </DataGrid.Columns>
  </DataGrid>

  <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,10,0,0">
    <TextBox x:Name="TxtFirstName" Width="150" Margin="0,0,8,0"/>
    <TextBox x:Name="TxtLastName"  Width="150" Margin="0,0,8,0"/>
    <TextBox x:Name="TxtYear"      Width="60"  Margin="0,0,8,0"/>
    <TextBox x:Name="TxtEmail"     Width="220" Margin="0,0,8,0"/>
    <Button x:Name="BtnAddStudent" Content="Přidat studenta" Click="BtnAddStudent_Click"/>
  </StackPanel>
</Grid>
```

**S komentáři:**

```xml
<Grid Margin="12">
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>     <!-- horní DataGrid -->
    <RowDefinition Height="Auto"/>  <!-- dolní formulář -->
  </Grid.RowDefinitions>

  <DataGrid x:Name="StudentsGrid"
            Grid.Row="0"
            AutoGenerateColumns="False"
            CanUserAddRows="False"
            IsReadOnly="False">
    <DataGrid.Columns>
      <DataGridTextColumn Header="ID"        Binding="{Binding Id}"        Width="70"/>
      <DataGridTextColumn Header="Jméno"     Binding="{Binding FirstName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
      <DataGridTextColumn Header="Příjmení"  Binding="{Binding LastName,  Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
      <DataGridTextColumn Header="Ročník"    Binding="{Binding Year,      Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="90"/>
      <DataGridTextColumn Header="E‑mail"    Binding="{Binding Email,     Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="2*"/>
      <DataGridTextColumn Header="Vytvořeno" Binding="{Binding CreatedAt, StringFormat={}{0:yyyy-MM-dd HH:mm:ss}}" Width="180"/>
    </DataGrid.Columns>
  </DataGrid>

  <!-- Jednoduchý formulář: 4 TextBoxy + tlačítko -->
  <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,10,0,0">
    <TextBox x:Name="TxtFirstName" Width="150" Margin="0,0,8,0" />
    <TextBox x:Name="TxtLastName"  Width="150" Margin="0,0,8,0" />
    <TextBox x:Name="TxtYear"      Width="60"  Margin="0,0,8,0" />
    <TextBox x:Name="TxtEmail"     Width="220" Margin="0,0,8,0" />
    <Button x:Name="BtnAddStudent" Content="Přidat studenta" Click="BtnAddStudent_Click"/>
  </StackPanel>
</Grid>
```

### 2B) `MainWindow.xaml.cs` – přidání studenta

**Bez komentářů:**

```csharp
private void BtnAddStudent_Click(object sender, RoutedEventArgs e)
{
    string firstName = (TxtFirstName.Text ?? string.Empty).Trim();
    string lastName  = (TxtLastName.Text  ?? string.Empty).Trim();
    string email     = (TxtEmail.Text     ?? string.Empty).Trim();
    int year = 1;
    int.TryParse((TxtYear.Text ?? string.Empty).Trim(), out year);

    var s = new Student
    {
        FirstName = firstName,
        LastName  = lastName,
        Year      = year,
        Email     = email,
        CreatedAt = DateTime.UtcNow
    };

    _db.Students.Add(s);
    _db.SaveChanges();

    _students.Add(s);
    StudentsGrid.SelectedItem = s;
    StudentsGrid.ScrollIntoView(s);

    TxtFirstName.Text = TxtLastName.Text = TxtEmail.Text = TxtYear.Text = string.Empty;
}
```

**S komentáři:**

```csharp
private void BtnAddStudent_Click(object sender, RoutedEventArgs e)
{
    // Načteme hodnoty z TextBoxů.
    // Operátor ?? je "null-coalescing": pokud je vlevo null, vrátí hodnotu vpravo.
    // Tedy: vezmi text, a když by náhodou byl null, použij prázdný řetězec.
    // Nakonec Trim() ořízne mezery na začátku/konci.
    string firstName = (TxtFirstName.Text ?? string.Empty).Trim();
    string lastName  = (TxtLastName.Text  ?? string.Empty).Trim();
    string email     = (TxtEmail.Text     ?? string.Empty).Trim();

    // Year načteme jako číslo; když se nepovede, zůstane výchozí hodnota 1.
    int year = 1;
    int.TryParse((TxtYear.Text ?? string.Empty).Trim(), out year);

    // Sestavíme entitu a doplníme čas vytvoření.
    var s = new Student
    {
        FirstName = firstName,
        LastName  = lastName,
        Year      = year,
        Email     = email,
        CreatedAt = DateTime.UtcNow
    };

    // Uložíme do DB, EF vygeneruje Id.
    _db.Students.Add(s);
    _db.SaveChanges();

    // Přidáme do kolekce pro DataGrid a vybereme novou položku.
    _students.Add(s);
    StudentsGrid.SelectedItem = s;
    StudentsGrid.ScrollIntoView(s);

    // Vyčistíme formulář.
    TxtFirstName.Text = TxtLastName.Text = TxtEmail.Text = TxtYear.Text = string.Empty;
}
```

**Pozn. k operátoru `??`:**  
`a ?? b` znamená: **když `a` není `null`, vezmi `a`, jinak vezmi `b`**.  
V našem případě: vezmi `TxtFirstName.Text`, a **kdyby náhodou bylo `null`**, použij `string.Empty`, čímž předejdeš výjimce při volání `.Trim()`.

Alternativa **bez `??`** pro výuku by mohla být:

```csharp
string firstName;
if (TxtFirstName.Text == null)
    firstName = string.Empty;
else
    firstName = TxtFirstName.Text.Trim();
```

---

## Úkol 3 — Úpravy, mazání a uložení změn

### 3A) `MainWindow.xaml` – tlačítka Uložit / Smazat

**Bez komentářů:**

```xml
<StackPanel Orientation="Horizontal" Margin="0,10,0,0">
  <Button x:Name="BtnSave"           Content="💾 Uložit"  Click="BtnSave_Click"   Margin="0,0,8,0"/>
  <Button x:Name="BtnDeleteSelected" Content="Smazat vybraného" Click="BtnDeleteSelected_Click"/>
</StackPanel>
```

**S komentáři:**

```xml
<StackPanel Orientation="Horizontal" Margin="0,10,0,0">
  <!-- Uloží rozeditované změny z mřížky do DB -->
  <Button x:Name="BtnSave"           Content="💾 Uložit"  Click="BtnSave_Click"   Margin="0,0,8,0"/>
  <!-- Smaže právě vybraný řádek -->
  <Button x:Name="BtnDeleteSelected" Content="Smazat vybraného" Click="BtnDeleteSelected_Click"/>
</StackPanel>
```

### 3B) `MainWindow.xaml.cs` – implementace Uložit / Smazat

**Bez komentářů:**

```csharp
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
```

**S komentáři:**

```csharp
private void BtnSave_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // DataGrid drží rozeditované buňky v paměti – commit přenese hodnoty do objektů
        StudentsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        StudentsGrid.CommitEdit(DataGridEditingUnit.Row, true);

        // Když EF neregistruje žádné změny, jen informujeme uživatele
        if (!_db.ChangeTracker.HasChanges())
        {
            MessageBox.Show("Žádné změny k uložení.", "Informace",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Uložit do DB
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
    // Zjistíme, co je vybráno v DataGridu
    var selected = StudentsGrid.SelectedItem as Student;
    if (selected == null)
    {
        MessageBox.Show("Nejprve vyberte studenta v tabulce.", "Upozornění",
            MessageBoxButton.OK, MessageBoxImage.Information);
        return;
    }

    // Potvrzení smazání
    var answer = MessageBox.Show("Opravdu smazat vybraného studenta?",
        "Potvrzení", MessageBoxButton.YesNo, MessageBoxImage.Question);

    if (answer != MessageBoxResult.Yes)
        return;

    // Smazat z DbContextu a uklidit i z UI kolekce
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
```

---

## Úkol 4 — Zabránit chybnému vstupu pro „Ročník“ (omezit 1–6)

Nejjednodušší je **nechat uživatele vybrat** z rozbalovacího seznamu místo volného psaní.

### 4A) `MainWindow.xaml` – ComboBox v mřížce i ve formuláři

**Bez komentářů:**

```xml
<Window xmlns:sys="clr-namespace:System;assembly=System.Runtime">
  <DataGrid.Columns>
    <DataGridComboBoxColumn Header="Ročník"
        SelectedItemBinding="{Binding Year, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}">
      <DataGridComboBoxColumn.ItemsSource>
        <x:Array Type="{x:Type sys:Int32}">
          <sys:Int32>1</sys:Int32>
          <sys:Int32>2</sys:Int32>
          <sys:Int32>3</sys:Int32>
          <sys:Int32>4</sys:Int32>
          <sys:Int32>5</sys:Int32>
          <sys:Int32>6</sys:Int32>
        </x:Array>
      </DataGridComboBoxColumn.ItemsSource>
    </DataGridComboBoxColumn>
  </DataGrid.Columns>

  <ComboBox x:Name="TxtYear">
    <ComboBox.ItemsSource>
      <x:Array Type="{x:Type sys:Int32}">
        <sys:Int32>1</sys:Int32>
        <sys:Int32>2</sys:Int32>
        <sys:Int32>3</sys:Int32>
        <sys:Int32>4</sys:Int32>
        <sys:Int32>5</sys:Int32>
        <sys:Int32>6</sys:Int32>
      </x:Array>
    </ComboBox.ItemsSource>
  </ComboBox>
</Window>
```

**S komentáři:**

```xml
<Window xmlns:sys="clr-namespace:System;assembly=System.Runtime">
  <!-- V mřížce: sloupec Ročník jako ComboBox, zdroj 1–6 -->
  <DataGrid.Columns>
    <DataGridComboBoxColumn Header="Ročník"
        SelectedItemBinding="{Binding Year, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}">
      <DataGridComboBoxColumn.ItemsSource>
        <x:Array Type="{x:Type sys:Int32}">
          <sys:Int32>1</sys:Int32>
          <sys:Int32>2</sys:Int32>
          <sys:Int32>3</sys:Int32>
          <sys:Int32>4</sys:Int32>
          <sys:Int32>5</sys:Int32>
          <sys:Int32>6</sys:Int32>
        </x:Array>
      </DataGridComboBoxColumn.ItemsSource>
    </DataGridComboBoxColumn>
  </DataGrid.Columns>

  <!-- Ve formuláři dole: stejné omezení -->
  <ComboBox x:Name="TxtYear"
            IsEditable="False" SelectedIndex="-1" ToolTip="Vyberte ročník 1–6">
    <ComboBox.ItemsSource>
      <x:Array Type="{x:Type sys:Int32}">
        <sys:Int32>1</sys:Int32>
        <sys:Int32>2</sys:Int32>
        <sys:Int32>3</sys:Int32>
        <sys:Int32>4</sys:Int32>
        <sys:Int32>5</sys:Int32>
        <sys:Int32>6</sys:Int32>
      </x:Array>
    </ComboBox.ItemsSource>
  </ComboBox>
</Window>
```

> V code‑behind můžeš stále číst `TxtYear.Text` a převést na číslo, nebo pohodlněji `if (TxtYear.SelectedItem is int year) { ... }`.

---

## Jak to celé navazuje

- **Model (`Student`)** popisuje data a pravidla (DataAnnotations).  
- **Kontext (`StudentContext`)** drží připojení k DB, vytvoří databázi a naplní ji vzorovými daty.  
- **UI (`MainWindow.xaml`)** zobrazuje data v **DataGridu** a poskytuje formulář & tlačítka.  
- **Code‑behind (`MainWindow.xaml.cs`)**: načítá data do `ObservableCollection`, napojuje je na DataGrid, obsluhuje kliknutí na tlačítka (přidat/uložit/smazat).  
- **PropertyChanged.Fody** řeší aktualizaci UI při změnách vlastností bez ručního psaní `INotifyPropertyChanged`.

---

## Spuštění

1. **Build** (Ctrl+Shift+B).  
2. **Start** (F5).  
3. Při prvním spuštění se vytvoří databáze a v mřížce uvidíš **seedovaná** data.  
4. Přidávej nové studenty formulářem. Upravuj buňky přímo v mřížce. Ulož změny tlačítkem **💾 Uložit**.  
5. Smaž vybraný řádek tlačítkem **Smazat vybraného**.

---

## Tipy k potížím

- **Změnil/a jsem strukturu tabulky a něco neodpovídá** → v **SQL Server Object Explorer** smaž danou LocalDB databázi, nebo změň `Initial Catalog` na nové jméno.  
- **Fody hlásí chybějící konfiguraci** → přidej `FodyWeavers.xml` (viz výše).  
- **DataGrid je prázdný** → zkontroluj `ItemsSource = _studentsView` a jestli `_students` opravdu plníš daty.

---

## Kompletní kód, který tento README používá (shrnutí)

Níže uvádím finální verze klíčových souborů **bez komentářů** — přesně v podobě, kterou projekt používá v jednotlivých úkolech.

### `Data/Student.cs`

```csharp
using PropertyChanged;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPF_Aplikace_s_db.Data
{
    [AddINotifyPropertyChangedInterface]
    public class Student
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string LastName { get; set; } = string.Empty;

        [Range(1, 6)]
        public int Year { get; set; }

        [StringLength(50)]
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

### `Data/StudentContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace WPF_Aplikace_s_db.Data
{
    public class StudentContext : DbContext
    {
        public DbSet<Student> Students => Set<Student>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    "Data Source=(localdb)\\MSSQLLocalDB;" +
                    "Initial Catalog=StudentDbDemo1;" +
                    "Integrated Security=True;" +
                    "TrustServerCertificate=True");
            }
        }

        public void EnsureCreatedAndSeed()
        {
            Database.EnsureCreated();
            SeedIfEmpty();
        }

        public void SeedIfEmpty()
        {
            if (!Students.Any())
            {
                var initial = new List<Student>
                {
                    new Student { FirstName="Jan",   LastName="Novák",    Year=1, Email="jan.novak@example.com" },
                    new Student { FirstName="Petr",  LastName="Svoboda",  Year=2, Email="petr.svoboda@example.com" },
                    new Student { FirstName="Karel", LastName="Černý",    Year=3, Email="karel.cerny@example.com" },
                    new Student { FirstName="Lucie", LastName="Malá",     Year=1, Email="lucie.mala@example.com" },
                    new Student { FirstName="Eva",   LastName="Bílá",     Year=2, Email="eva.bila@example.com" },
                    new Student { FirstName="Adam",  LastName="Zelený",   Year=3, Email="adam.zeleny@example.com" },
                    new Student { FirstName="Tomáš", LastName="Dvořák",   Year=1, Email="tomas.dvorak@example.com" },
                    new Student { FirstName="Marie", LastName="Veselá",   Year=2, Email="marie.vesela@example.com" },
                    new Student { FirstName="Jana",  LastName="Horáková", Year=3, Email="jana.horakova@example.com" },
                    new Student { FirstName="Filip", LastName="Král",     Year=1, Email="filip.kral@example.com" }
                };
                Students.AddRange(initial);
                SaveChanges();
            }
        }
    }
}
```

### `MainWindow.xaml.cs` (části důležité pro úkoly 1–3)

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPF_Aplikace_s_db.Data;
using System;

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

        protected override void OnClosed(EventArgs e)
        {
            _db.Dispose();
            base.OnClosed(e);
        }

        private void BtnAddStudent_Click(object sender, RoutedEventArgs e)
        {
            string firstName = (TxtFirstName.Text ?? string.Empty).Trim();
            string lastName  = (TxtLastName.Text  ?? string.Empty).Trim();
            string email     = (TxtEmail.Text     ?? string.Empty).Trim();
            int year = 1;
            int.TryParse((TxtYear.Text ?? string.Empty).Trim(), out year);

            var s = new Student
            {
                FirstName = firstName,
                LastName  = lastName,
                Year      = year,
                Email     = email,
                CreatedAt = DateTime.UtcNow
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
            catch (Exception ex)
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
            catch (Exception ex)
            {
                MessageBox.Show("Smazání se nepodařilo.\n\n" + ex, "Chyba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
```

---

## Licence

Tento výukový materiál můžeš volně používat v rámci výuky. Uvítáme hvězdičku ⭐ na GitHubu, pokud ti pomohl.
