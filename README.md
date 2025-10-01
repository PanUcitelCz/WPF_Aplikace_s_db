# WPF (.NET 9) + EF Core: Students CRUD — výukový projekt

Tento **výukový README** krok‑za‑krokem ukazuje, jak ve Visual Studiu 2022 vytvořit **WPF Application (.NET)** s databází přes **Entity Framework Core (SQL Server LocalDB)**. Projekt umí:

1) **Vypsat** tabulku studentů do `DataGridu`  
2) **Přidávat** nové záznamy z formuláře (bez validace)  
3) **Upravovat** a **mazat** záznamy přímo v mřížce + **Uložit** změny do DB (s potvrzením)  
4) **Zabránit chybnému vstupu** u „Ročník“ tím, že povolíme **pouze výběr 1–6** z rozbalovacího seznamu

> Vše je **klikací** ve VS 2022 (žádný terminál). Šablona se v českém VS typicky jmenuje **„Aplikace WPF“** / v anglickém **„WPF Application“** pro **.NET** (neplést se starou **„WPF App (.NET Framework)“**).

> README vychází z těchto souborů projektu: `StudentContext.cs` (připojení a seed), `Student.cs` (model), `MainWindow.xaml.cs` (načtení a CRUD).

---

## 0) Příprava projektu (klikací postup)

### 0.1 Vytvoření projektu

1. Otevři **Visual Studio 2022**.  
2. **Create a new project** → vyber **WPF Application** (pro .NET) → **Next**.  
3. **Project name** zadej např. `WPF_Aplikace_s_db`.  
4. **Framework** nastav na **.NET 9.0** → **Create**.

### 0.2 Instalace NuGet balíčků

V **Solution Exploreru** klikni pravým na projekt → **Manage NuGet Packages…** → záložka **Browse** a nainstaluj postupně:

- **Microsoft.EntityFrameworkCore.SqlServer** – poskytovatel EF Core pro SQL Server (i **LocalDB**) – aplikace díky němu mluví se SQL Serverem.  
- **Microsoft.EntityFrameworkCore.Tools** – design‑time nástroje (pomáhají s návrhem/migracemi; i když zde používáme `EnsureCreated`, hodí se do budoucna).  
- **PropertyChanged.Fody** *(nainstaluje i **Fody**)* – Weaver, který automaticky přidá `INotifyPropertyChanged` třídám označeným `[AddINotifyPropertyChangedInterface]`. Změny vlastností se pak hned promítnou v UI bez ručního psaní notifikací.

### 0.3 Nastavení Fody (pokud si o to projekt řekne)
Pokud Fody vyžádá konfigurační soubor, přidej do projektu **`FodyWeavers.xml`**:

```xml
<Weavers>
  <PropertyChanged />
</Weavers>
```

> **Databáze**: používáme SQL Server **LocalDB**. Při prvním spuštění se DB vytvoří a naplní vzorovými daty.
> Kdybychom později měnili schéma a chtěli „čistý start“, ve VS otevři **SQL Server Object Explorer** a databázi smaž,
> nebo v connection stringu změň `Initial Catalog` na nové jméno.

---

# Učební postup po úkolech
Každý krok ukazujeme **nejdřív bez komentářů** a hned **poté s komentáři každého řádku**.  
Kód je sladěný s touto výukovou verzí projektu.

---

## Úkol 1 — Vypsat data ze `StudentContext` do `DataGridu`

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

**S komentáři (každý řádek):**

```csharp
using PropertyChanged;                             // weaver: automaticky přidá INotifyPropertyChanged
using System;                                      // základní typy (DateTime)
using System.ComponentModel.DataAnnotations;       // validace (Required, StringLength, Range)
using System.ComponentModel.DataAnnotations.Schema;// mapování do DB (DatabaseGenerated)

namespace WPF_Aplikace_s_db.Data                    // jmenný prostor pro data
{
    [AddINotifyPropertyChangedInterface]            // Fody: UI se samo překreslí při změně vlastností
    public class Student                            // entita => řádek v tabulce Students
    {
        [Key]                                       // primární klíč
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Id generuje DB (IDENTITY)
        public int Id { get; set; }                 // unikátní číslo

        [Required]                                   // povinné pole
        [StringLength(30)]                           // max 30 znaků
        public string FirstName { get; set; } = string.Empty; // křestní jméno

        [Required]
        [StringLength(30)]
        public string LastName { get; set; } = string.Empty;  // příjmení

        [Range(1, 6)]                                // rozsah 1–6
        public int Year { get; set; }                // ročník

        [StringLength(50)]                           // max 50 znaků
        public string Email { get; set; } = string.Empty;     // e‑mail

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // čas vytvoření (UTC)
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

**S komentáři (každý řádek):**

```csharp
using Microsoft.EntityFrameworkCore;        // EF Core: DbContext, DbSet, UseSqlServer
using System.Collections.Generic;           // List<T> pro seed
using System.Linq;                           // LINQ (Any, OrderBy, ToList)

namespace WPF_Aplikace_s_db.Data             // data: modely + kontext
{
    public class StudentContext : DbContext  // DbContext = připojení k DB + sada entit
    {
        public DbSet<Student> Students => Set<Student>(); // tabulka Students (CRUD přes EF)

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) // nastavení připojení
        {
            if (!optionsBuilder.IsConfigured)                 // jen pokud není nastavené jinde
            {
                optionsBuilder.UseSqlServer(                  // SQL Server provider
                    "Data Source=(localdb)\\MSSQLLocalDB;" +  // LocalDB instance
                    "Initial Catalog=StudentDbDemo1;" +       // jméno DB
                    "Integrated Security=True;" +             // Windows autentizace
                    "TrustServerCertificate=True");           // bez potíží s certifikátem
            }
        }

        public void EnsureCreatedAndSeed()    // zavoláme při startu
        {
            Database.EnsureCreated();         // vytvoří DB (pokud ještě není)
            SeedIfEmpty();                    // naplní ukázkovými daty
        }

        public void SeedIfEmpty()             // jen pokud je tabulka prázdná
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
                Students.AddRange(initial);   // vlož do kontextu
                SaveChanges();                // ulož do DB
            }
        }
    }
}
```

### 1C) `MainWindow.xaml` — DataGrid se sloupci

**Bez komentářů (minimální mřížka):**

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

**S komentáři (každý řádek):**

```xml
<Window x:Class="WPF_Aplikace_s_db.MainWindow"               <!-- code-behind třída pro toto okno -->
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"  <!-- WPF prvky -->
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"             <!-- XAML rozšíření -->
        Title="Students" Height="450" Width="800">                          <!-- titul a velikost -->
  <Grid Margin="12">                                                        <!-- okraj uvnitř okna -->
    <DataGrid x:Name="StudentsGrid"                                         <!-- mřížka studentů -->
              AutoGenerateColumns="False"                                   <!-- sloupce nadefinujeme ručně -->
              CanUserAddRows="False"                                        <!-- bez prázdného řádku pro přidání -->
              IsReadOnly="False">                                           <!-- povolíme editaci buněk -->
      <DataGrid.Columns>                                                    <!-- ruční definice sloupců -->
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

### 1D) `MainWindow.xaml.cs` — **pouze načtení dat** (bez tlačítek; to přijde v úk. 2–3)

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

**S komentáři (každý řádek):**

```csharp
using System.Collections.ObjectModel;                 // ObservableCollection = živá kolekce pro UI
using System.ComponentModel;                         // ICollectionView + třídění/filtrování
using System.Linq;                                   // LINQ (OrderBy, ToList)
using System.Windows;                                // Window, základní WPF typy
using System.Windows.Data;                           // CollectionViewSource.GetDefaultView
using WPF_Aplikace_s_db.Data;                        // modely + StudentContext

namespace WPF_Aplikace_s_db                           // jmenný prostor aplikace
{
    public partial class MainWindow : Window         // okno MainWindow (navázané na XAML)
    {
        private readonly StudentContext _db = new StudentContext();       // EF Core kontext
        private readonly ObservableCollection<Student> _students = new(); // kolekce pro grid
        private ICollectionView _studentsView;                             // wrapper pro třídění atd.

        public MainWindow()                               // konstruktor okna
        {
            InitializeComponent();                        // vygeneruje UI prvky z XAML

            _db.EnsureCreatedAndSeed();                   // vytvoř DB a naplň výchozími daty (pokud prázdná)

            foreach (var s in _db.Students.OrderBy(x => x.Id).ToList()) // načti studenty seřazené dle Id
                _students.Add(s);                         // vlož do kolekce (DataGrid se sám obnoví)

            _studentsView = CollectionViewSource.GetDefaultView(_students); // pohled nad kolekcí
            _studentsView.SortDescriptions.Clear();                         // pro jistotu vyčisti třídění
            _studentsView.SortDescriptions.Add(                             
                new SortDescription(nameof(Student.Id), ListSortDirection.Ascending)); // seřaď podle Id

            StudentsGrid.ItemsSource = _studentsView;     // propojení gridu s daty
        }

        protected override void OnClosed(System.EventArgs e) // zavírání okna
        {
            _db.Dispose();                                // uvolni DB připojení
            base.OnClosed(e);                             // základní chování
        }
    }
}
```

---

## Úkol 2 — Přidávání záznamu (bez validace)

### 2A) `MainWindow.xaml` — přidáme jednoduchý formulář a tlačítko

**Bez komentářů:**

```xml
<Grid Margin="12">
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
    <RowDefinition Height="Auto"/>
  </Grid.RowDefinitions>

  <DataGrid x:Name="StudentsGrid" Grid.Row="0"
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

**S komentáři (každý řádek):**

```xml
<Grid Margin="12">                                                <!-- hlavní mřížka s okrajem -->
  <Grid.RowDefinitions>                                           <!-- rozdělení UI na 2 řádky -->
    <RowDefinition Height="*"/>                                   <!-- horní DataGrid -->
    <RowDefinition Height="Auto"/>                                <!-- dolní formulář -->
  </Grid.RowDefinitions>

  <DataGrid x:Name="StudentsGrid" Grid.Row="0"
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

  <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,10,0,0">  <!-- jednoduchý formulář -->
    <TextBox x:Name="TxtFirstName" Width="150" Margin="0,0,8,0"/>       <!-- jméno -->
    <TextBox x:Name="TxtLastName"  Width="150" Margin="0,0,8,0"/>       <!-- příjmení -->
    <TextBox x:Name="TxtYear"      Width="60"  Margin="0,0,8,0"/>       <!-- ročník (zatím bez omezení) -->
    <TextBox x:Name="TxtEmail"     Width="220" Margin="0,0,8,0"/>       <!-- e‑mail -->
    <Button x:Name="BtnAddStudent" Content="Přidat studenta" Click="BtnAddStudent_Click"/> <!-- přidá záznam -->
  </StackPanel>
</Grid>
```

### 2B) `MainWindow.xaml.cs` — **přidání studenta**

**Bez komentářů (z tvého finálního kódu):**

```csharp
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
```

**S komentáři (každý řádek):**

```csharp
private void BtnAddStudent_Click(object sender, RoutedEventArgs e)      // klik na "Přidat studenta"
{
    string firstName = (TxtFirstName.Text ?? string.Empty).Trim();       // načti jméno; pokud null → ""
    string lastName  = (TxtLastName.Text  ?? string.Empty).Trim();       // načti příjmení
    string email     = (TxtEmail.Text     ?? string.Empty).Trim();       // načti e‑mail
    int year = 1;                                                        // výchozí ročník
    int.TryParse((TxtYear.Text ?? string.Empty).Trim(), out year);       // pokus o převod na číslo (když selže, zůstane 1)

    var s = new Student                                                  // připrav novou entitu
    {
        FirstName = firstName,                                           // vyplň data z formuláře
        LastName  = lastName,
        Year      = year,
        Email     = email,
        CreatedAt = System.DateTime.UtcNow                               // čas vytvoření v UTC
    };

    _db.Students.Add(s);                                                 // přidej do kontextu
    _db.SaveChanges();                                                   // ulož do DB (vygeneruje Id)

    _students.Add(s);                                                    // přidej do UI kolekce
    StudentsGrid.SelectedItem = s;                                       // vyber nového studenta
    StudentsGrid.ScrollIntoView(s);                                      // posuň grid na záznam (viditelnost)

    TxtFirstName.Text = TxtLastName.Text = TxtEmail.Text = TxtYear.Text = string.Empty; // vyčisti formulář
}
```

> **Co znamená `??` (null‑coalescing operátor):** `A ?? B` vrátí `A`, pokud není `null`, jinak vrátí `B`.  
> Díky tomu `.Trim()` nikdy nepadne na `NullReferenceException` (když `Text` náhodou vrátí `null`).

---

## Úkol 3 — Úpravy v mřížce, ukládání s potvrzením a mazání

### 3A) `MainWindow.xaml` — tlačítka Uložit / Smazat

**Bez komentářů:**

```xml
<StackPanel Orientation="Horizontal" Margin="0,10,0,0">
  <Button x:Name="BtnSave"           Content="💾 Uložit"          Click="BtnSave_Click"   Margin="0,0,8,0"/>
  <Button x:Name="BtnDeleteSelected" Content="Smazat vybraného"  Click="BtnDeleteSelected_Click"/>
</StackPanel>
```

**S komentáři (každý řádek):**

```xml
<StackPanel Orientation="Horizontal" Margin="0,10,0,0">              <!-- vodorovný panel s tlačítky -->
  <Button x:Name="BtnSave"           Content="💾 Uložit"              <!-- uloží změny do DB (s potvrzením) -->
          Click="BtnSave_Click"   Margin="0,0,8,0"/>
  <Button x:Name="BtnDeleteSelected" Content="Smazat vybraného"       <!-- smaže vybraného studenta (s potvrzením) -->
          Click="BtnDeleteSelected_Click"/>
</StackPanel>
```

### 3B) `MainWindow.xaml.cs` — **uložit změny** (zjednodušená verze s potvrzením)

**Bez komentářů (z tvého finálního kódu):**

```csharp
private void BtnSave_Click(object sender, RoutedEventArgs e)
{
    var selected = StudentsGrid.SelectedItem as Student;
    if (selected != null)
    {
        var result = MessageBox.Show(this,
            $"Opravdu chcete údaje studenta {selected.FirstName} {selected.LastName}?",
            "Uložit?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _db.SaveChanges();
        }
    }
}
```

**S komentáři (každý řádek):**

```csharp
private void BtnSave_Click(object sender, RoutedEventArgs e)        // klik na "Uložit"
{
    var selected = StudentsGrid.SelectedItem as Student;            // získej aktuálně vybraného studenta
    if (selected != null)                                           // pokud je něco vybráno
    {
        var result = MessageBox.Show(this,                          // zobraz potvrzovací dialog
            $"Opravdu chcete údaje studenta {selected.FirstName} {selected.LastName}?",
            "Uložit?",
            MessageBoxButton.YesNo,                                 // tlačítka Ano/Ne
            MessageBoxImage.Question);                              // ikonka otazníku

        if (result == MessageBoxResult.Yes)                         // jen pokud uživatel potvrdil
        {
            _db.SaveChanges();                                      // ulož veškeré změny trackované EF kontextem
        }
    }
}
```

> **Poznámka didakticky:** Tato minimalistická verze nevolá `CommitEdit` na `DataGridu`. Pokud uživatel zrovna **edituje buňku** a ještě **nestiskl Enter / nepřesunul fokus**, editace může být stále jen v editoru a **nemusí být commitnuta** do objektu. Doporuč: po dopsání do buňky stisknout Enter nebo kliknout mimo řádek; případně lze doplnit:
>
> ```csharp
> StudentsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
> StudentsGrid.CommitEdit(DataGridEditingUnit.Row, true);
> ```

### 3C) `MainWindow.xaml.cs` — **smazat vybraného** (s potvrzením)

**Bez komentářů (z tvého finálního kódu):**

```csharp
private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
{
    var selected = StudentsGrid.SelectedItem as Student;
    if (selected != null)
    {
        var result = MessageBox.Show(this,
            $"Opravdu smazat studenta {selected.FirstName} {selected.LastName}?",
            "Smazat studenta",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _db.Students.Remove(selected);
            _db.SaveChanges();
            _students.Remove(selected);
        }
    }
}
```

**S komentáři (každý řádek):**

```csharp
private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e) // klik na "Smazat vybraného"
{
    var selected = StudentsGrid.SelectedItem as Student;               // co je právě vybráno
    if (selected != null)                                              // pokud existuje výběr
    {
        var result = MessageBox.Show(this,                              // potvrzovací dialog
            $"Opravdu smazat studenta {selected.FirstName} {selected.LastName}?",
            "Smazat studenta",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)                             // pokračuj jen při potvrzení
        {
            _db.Students.Remove(selected);                              // odeber z EF kontextu
            _db.SaveChanges();                                          // potvrď v DB
            _students.Remove(selected);                                 // smaž i z UI kolekce (okamžitá změna v DataGridu)
        }
    }
}
```

---

## Úkol 4 — Zabránit chybnému vstupu pro „Ročník“ (omezit na 1–6)

Namísto volného psaní použijeme **rozbalovací seznam** s pevnými hodnotami 1–6. Přidáme to **jak do mřížky**, tak **do formuláře**.

### 4A) `MainWindow.xaml` — ComboBox v mřížce i ve formuláři

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

  <ComboBox x:Name="TxtYear" IsEditable="False" SelectedIndex="-1" ToolTip="Vyberte ročník 1–6">
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

**S komentáři (každý řádek):**

```xml
<Window xmlns:sys="clr-namespace:System;assembly=System.Runtime">  <!-- import System.Int32 do XAML -->
  <DataGrid.Columns>                                               <!-- sloupce gridu -->
    <DataGridComboBoxColumn Header="Ročník"                        <!-- sloupec jako rozbalovací seznam -->
        SelectedItemBinding="{Binding Year, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"> <!-- dvoucestný bind na Year -->
      <DataGridComboBoxColumn.ItemsSource>                         <!-- pevný seznam hodnot 1–6 -->
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

  <ComboBox x:Name="TxtYear" IsEditable="False" SelectedIndex="-1" ToolTip="Vyberte ročník 1–6"> <!-- formulář dole -->
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

> V code‑behind lze hodnotu číst i bezpečněji: `if (TxtYear.SelectedItem is int y) { … }`.

---

## Jak to celé zapadá dohromady
- **Model (`Student`)**: popisuje sloupce a základní pravidla (DataAnnotations).  
- **Kontext (`StudentContext`)**: připojení k DB, vytvoření DB a seed dat.  
- **UI (`MainWindow.xaml`)**: DataGrid pro zobrazení a editaci + formulář (TextBox/ComboBox) pro přidání.  
- **Code‑behind (`MainWindow.xaml.cs`)**: načtení dat do `ObservableCollection`, uložení/mazání s potvrzením.  
- **PropertyChanged.Fody**: automatické notifikace změn do UI.

---

## Spuštění
1. **Build** (Ctrl+Shift+B).  
2. **Start** (F5).  
3. Při prvním spuštění se vytvoří LocalDB a uvidíš **seedovaná** data.  
4. Přidávej nové studenty formulářem. Upravuj buňky přímo v mřížce. Ulož změny tlačítkem **💾 Uložit**.  
5. Smaž vybraný řádek tlačítkem **Smazat vybraného**.

---

## Tipy k potížím
- **Úprava buňky a okamžité uložení**: pokud uživatel nestiskl Enter / nezměnil fokus, editace nemusí být commitnutá. Pomůže Enter/klik mimo, nebo doplnit `StudentsGrid.CommitEdit(...)`.  
- **Změnil/a jsem schéma a něco nesedí** → v **SQL Server Object Explorer** smaž LocalDB DB, nebo změň `Initial Catalog` na jiné jméno.  
- **Fody hlásí konfiguraci** → přidej `FodyWeavers.xml` (viz výše).  
- **DataGrid je prázdný** → zkontroluj `StudentsGrid.ItemsSource = _studentsView` a naplnění `_students`.

---

## Shrnutí (verze bez komentářů — minimální jádro)
> Níže jsou finální verze klíčových souborů bez komentářů, na které se v úkolech odkazujeme.

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

### `MainWindow.xaml.cs` (verze se zjednodušeným Uložit/Smazat)
```csharp
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
            var selected = StudentsGrid.SelectedItem as Student;
            if (selected != null)
            {
                var result = MessageBox.Show(this,
                    $"Opravdu chcete údaje studenta {selected.FirstName} {selected.LastName}?",
                    "Uložit?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _db.SaveChanges();
                }
            }
        }

        private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = StudentsGrid.SelectedItem as Student;
            if (selected != null)
            {
                var result = MessageBox.Show(this,
                    $"Opravdu smazat studenta {selected.FirstName} {selected.LastName}?",
                    "Smazat studenta",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _db.Students.Remove(selected);
                    _db.SaveChanges();
                    _students.Remove(selected);
                }
            }
        }
    }
}
```

---

## Licence
Tento výukový materiál můžeš volně používat v rámci výuky. Pokud ti pomohl, přidej ⭐ na GitHubu.
