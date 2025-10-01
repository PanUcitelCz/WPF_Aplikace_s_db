# WPF (.NET 9) + EF Core: Students CRUD — výukový projekt

Tento **výukový README** krok‑za‑krokem ukazuje, jak ve Visual Studiu 2022 vytvořit **WPF Application (.NET)** s databází přes **Entity Framework Core (SQL Server LocalDB)**. Projekt umí:

1) **Vypsat** tabulku studentů do `DataGridu`  
2) **Přidávat** nové záznamy z formuláře (bez validace)  
3) **Upravovat** a **mazat** záznamy přímo v mřížce + **Uložit** změny do DB  
4) **Zabránit chybnému vstupu** u „Ročník“ tím, že povolíme **pouze výběr 1–6** z rozbalovacího seznamu

> Vše je **klikací** ve VS 2022 (žádný terminál). Šablona se v českém VS typicky jmenuje **„Aplikace WPF“** / v anglickém **„WPF Application“** pro **.NET** (neplést se starou **„WPF App (.NET Framework)“**).

---

## 0) Příprava projektu (klikací postup)

### 0.1 Vytvoření projektu

1. Otevři **Visual Studio 2022**.  
2. **Create a new project** → vyber **WPF Application** (pro .NET) → **Next**.  
3. **Project name** zadej např. `WPF_Aplikace_s_db`.  
4. **Framework** nastav na **.NET 9.0** → **Create**.

### 0.2 Instalace NuGet balíčků

V **Solution Exploreru** klikni pravým na projekt → **Manage NuGet Packages…** → záložka **Browse** a nainstaluj postupně:

- **Microsoft.EntityFrameworkCore.SqlServer**  
  Poskytovatel EF Core pro SQL Server (včetně **LocalDB**). Díky němu aplikace mluví se SQL Serverem.
- **Microsoft.EntityFrameworkCore.Tools**  
  Design‑time nástroje EF (pomáhají s návrhem a migracemi). V tomto kurzu používáme `EnsureCreated`, ale nástroj se hodí do budoucna.
- **PropertyChanged.Fody** *(nainstaluje i balíček **Fody**)*  
  Weaver, který automaticky přidá `INotifyPropertyChanged` třídám označeným atributem `[AddINotifyPropertyChangedInterface]`. Změny vlastností se tak ihned promítají do UI bez ručního psaní notifikací.

### 0.3 Nastavení Fody (pokud si o to projekt řekne)
Pokud Fody vyžádá konfigurační soubor, přidej do projektu nový soubor **`FodyWeavers.xml`** s obsahem:

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
using PropertyChanged;                             // importuje weaver pro automatické INotifyPropertyChanged
using System;                                      // základní typy, např. DateTime
using System.ComponentModel.DataAnnotations;       // atributy pro validace (Required, StringLength, Range)
using System.ComponentModel.DataAnnotations.Schema;// atributy pro mapování do DB (DatabaseGenerated)

namespace WPF_Aplikace_s_db.Data                    // jmenný prostor modelů a kontextu
{
    [AddINotifyPropertyChangedInterface]            // Fody: automaticky vytvoří notifikace změn pro všechny vlastnosti
    public class Student                            // entita "Student" = 1 řádek v tabulce Students
    {
        [Key]                                       // primární klíč
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Id generuje databáze (IDENTITY)
        public int Id { get; set; }                 // číslo záznamu

        [Required]                                   // povinné
        [StringLength(30)]                           // max 30 znaků
        public string FirstName { get; set; } = string.Empty; // křestní jméno

        [Required]
        [StringLength(30)]
        public string LastName { get; set; } = string.Empty;  // příjmení

        [Range(1, 6)]                                // povolený rozsah hodnot 1–6
        public int Year { get; set; }                // ročník

        [StringLength(50)]                           // max 50 znaků
        public string Email { get; set; } = string.Empty;     // e-mail

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
using System.Collections.Generic;           // List<T> pro seed dat
using System.Linq;                           // LINQ (Any, ToList, OrderBy)

namespace WPF_Aplikace_s_db.Data             // jmenný prostor datových tříd
{
    public class StudentContext : DbContext  // kontext EF Core – drží připojení a sadu entit
    {
        public DbSet<Student> Students => Set<Student>(); // virtuální tabulka Students (CRUD přes EF)

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) // nastavení připojení
        {
            if (!optionsBuilder.IsConfigured)                 // jen pokud ještě není nastavené jinde
            {
                optionsBuilder.UseSqlServer(                  // SQL Server poskytovatel
                    "Data Source=(localdb)\\MSSQLLocalDB;" +  // LocalDB instance (lokální SQL Server)
                    "Initial Catalog=StudentDbDemo1;" +       // název databáze
                    "Integrated Security=True;" +             // integrované přihlášení Windows
                    "TrustServerCertificate=True");           // pro LocalDB bez potíží s certifikátem
            }
        }

        public void EnsureCreatedAndSeed()    // pomocná metoda volaná při startu aplikace
        {
            Database.EnsureCreated();         // vytvoří databázi (pokud neexistuje)
            SeedIfEmpty();                    // naplní tabulku Students vzorovými daty
        }

        public void SeedIfEmpty()             // seed jen když je tabulka prázdná
        {
            if (!Students.Any())              // žádní studenti v DB?
            {
                var initial = new List<Student> // připrav vzorovou sadu
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
                SaveChanges();                // ulož do databáze
            }
        }
    }
}
```

### 1C) `MainWindow.xaml` — DataGrid se sloupci

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

**S komentáři (každý řádek):**

```xml
<Window x:Class="WPF_Aplikace_s_db.MainWindow"               <!-- třída code-behind, která toto okno obsluhuje -->
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"  <!-- základní WPF prvky -->
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"             <!-- XAML rozšíření (x:Name apod.) -->
        Title="Students" Height="450" Width="800">                          <!-- titulek okna a velikost -->
  <Grid Margin="12">                                                        <!-- základní rozložení s okrajem -->
    <DataGrid x:Name="StudentsGrid"                                         <!-- pojmenovaný DataGrid pro bind -->
              AutoGenerateColumns="False"                                   <!-- sloupce definujeme ručně -->
              CanUserAddRows="False"                                        <!-- skryje prázdný řádek pro přidání -->
              IsReadOnly="False">                                           <!-- buňky půjdou editovat -->
      <DataGrid.Columns>                                                    <!-- ručně definované sloupce -->
        <DataGridTextColumn Header="ID"        Binding="{Binding Id}"        Width="70"/>         <!-- Id -->
        <DataGridTextColumn Header="Jméno"     Binding="{Binding FirstName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="*"/>  <!-- FirstName -->
        <DataGridTextColumn Header="Příjmení"  Binding="{Binding LastName,  Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="*"/>  <!-- LastName -->
        <DataGridTextColumn Header="Ročník"    Binding="{Binding Year,      Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="90"/> <!-- Year -->
        <DataGridTextColumn Header="E‑mail"    Binding="{Binding Email,     Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="2*"/> <!-- Email -->
        <DataGridTextColumn Header="Vytvořeno" Binding="{Binding CreatedAt, StringFormat={}{0:yyyy-MM-dd HH:mm:ss}}" Width="180"/>         <!-- timestamp -->
      </DataGrid.Columns>
    </DataGrid>
  </Grid>
</Window>
```

### 1D) `MainWindow.xaml.cs` — načtení dat do mřížky

**Bez komentářů:**

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
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
    }
}
```

**S komentáři (každý řádek):**

```csharp
using System.Collections.ObjectModel;                 // ObservableCollection pro živé datové zdroje v UI
using System.ComponentModel;                         // ICollectionView a třídění
using System.Linq;                                   // LINQ (OrderBy, ToList)
using System.Windows;                                // Window, MessageBox apod.
using System.Windows.Data;                           // CollectionViewSource
using WPF_Aplikace_s_db.Data;                        // náš model a kontext
using System;                                        // EventArgs

namespace WPF_Aplikace_s_db                           // jmenný prostor aplikace
{
    public partial class MainWindow : Window         // hlavní okno aplikace
    {
        private readonly StudentContext _db = new StudentContext();       // EF Core kontext (DB připojení)
        private readonly ObservableCollection<Student> _students = new(); // kolekce, na kterou se binduje DataGrid
        private ICollectionView _studentsView;                             // wrapper pro třídění/filtrování

        public MainWindow()                               // konstruktor okna
        {
            InitializeComponent();                        // vytvoří vizuální prvky z XAML

            _db.EnsureCreatedAndSeed();                   // zajistí DB a naplní vzorovými daty

            foreach (var s in _db.Students.OrderBy(x => x.Id).ToList()) // načte studenty seřazené dle Id
                _students.Add(s);                         // vloží je do pozorovatelné kolekce

            _studentsView = CollectionViewSource.GetDefaultView(_students); // vytvoří pohled nad kolekcí
            _studentsView.SortDescriptions.Clear();                         // smaže původní třídění
            _studentsView.SortDescriptions.Add(                             // přidá třídění podle Id vzestupně
                new SortDescription(nameof(Student.Id), ListSortDirection.Ascending));

            StudentsGrid.ItemsSource = _studentsView;     // napojí DataGrid na náš pohled
        }

        protected override void OnClosed(EventArgs e)     // při zavření okna
        {
            _db.Dispose();                                // zavře DB připojení
            base.OnClosed(e);                             // zavolá základní chování
        }
    }
}
```

---

## Úkol 2 — Přidávání záznamu (bez validace)

### 2A) `MainWindow.xaml` — jednoduchý formulář dole + tlačítko

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
  <Grid.RowDefinitions>                                           <!-- rozdělení na dva řádky -->
    <RowDefinition Height="*"/>                                   <!-- 1. řádek: DataGrid -->
    <RowDefinition Height="Auto"/>                                <!-- 2. řádek: formulář -->
  </Grid.RowDefinitions>

  <DataGrid x:Name="StudentsGrid" Grid.Row="0"                    <!-- mřížka studentů nahoře -->
            AutoGenerateColumns="False"                           <!-- sloupce definujeme níže -->
            CanUserAddRows="False"                                <!-- žádný prázdný „plus“ řádek -->
            IsReadOnly="False">                                   <!-- editace buněk povolena -->
    <DataGrid.Columns>                                            <!-- definice sloupců -->
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

### 2B) `MainWindow.xaml.cs` — přidání studenta

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

**S komentáři (každý řádek):**

```csharp
private void BtnAddStudent_Click(object sender, RoutedEventArgs e)  // obsluha kliknutí na tlačítko
{
    string firstName = (TxtFirstName.Text ?? string.Empty).Trim();   // přečte text; pokud null → "", ořízne mezery
    string lastName  = (TxtLastName.Text  ?? string.Empty).Trim();   // dtto pro příjmení
    string email     = (TxtEmail.Text     ?? string.Empty).Trim();   // dtto pro e‑mail
    int year = 1;                                                    // výchozí ročník
    int.TryParse((TxtYear.Text ?? string.Empty).Trim(), out year);   // pokusí se převést na číslo, v případě neúspěchu zůstane 1

    var s = new Student                                             // vytvoří novou entitu
    {
        FirstName = firstName,                                      // vyplní vlastnosti
        LastName  = lastName,
        Year      = year,
        Email     = email,
        CreatedAt = DateTime.UtcNow                                 // aktuální čas v UTC
    };

    _db.Students.Add(s);                                            // přidá do kontextu
    _db.SaveChanges();                                              // uloží do databáze (vygeneruje Id)

    _students.Add(s);                                               // přidá do kolekce zobrazené v DataGridu
    StudentsGrid.SelectedItem = s;                                  // vybere nový řádek
    StudentsGrid.ScrollIntoView(s);                                 // posune mřížku na nový řádek

    TxtFirstName.Text = TxtLastName.Text = TxtEmail.Text = TxtYear.Text = string.Empty; // vyčistí formulář
}
```

> **Co znamená `??` (null‑coalescing operátor):** `A ?? B` znamená: pokud je `A` **není null**, vezmi `A`; jinak vezmi **`B`**.
> Používáme ho, aby volání `.Trim()` nikdy nespadlo na `NullReferenceException`.

---

## Úkol 3 — Úpravy v mřížce, mazání a uložení změn

### 3A) `MainWindow.xaml` — tlačítka Uložit / Smazat

**Bez komentářů:**

```xml
<StackPanel Orientation="Horizontal" Margin="0,10,0,0">
  <Button x:Name="BtnSave"           Content="💾 Uložit"  Click="BtnSave_Click"   Margin="0,0,8,0"/>
  <Button x:Name="BtnDeleteSelected" Content="Smazat vybraného" Click="BtnDeleteSelected_Click"/>
</StackPanel>
```

**S komentáři (každý řádek):**

```xml
<StackPanel Orientation="Horizontal" Margin="0,10,0,0">                <!-- vodorovný panel s tlačítky -->
  <Button x:Name="BtnSave"           Content="💾 Uložit"               <!-- uloží změny do DB -->
          Click="BtnSave_Click"   Margin="0,0,8,0"/>
  <Button x:Name="BtnDeleteSelected" Content="Smazat vybraného"        <!-- smaže označený řádek -->
          Click="BtnDeleteSelected_Click"/>
</StackPanel>
```

### 3B) `MainWindow.xaml.cs` — implementace Uložit / Smazat

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

**S komentáři (každý řádek):**

```csharp
private void BtnSave_Click(object sender, RoutedEventArgs e)         // uložit změny z mřížky
{
    try                                                              // ošetření výjimek
    {
        StudentsGrid.CommitEdit(DataGridEditingUnit.Cell, true);     // commit buněk (přenese z editoru do objektu)
        StudentsGrid.CommitEdit(DataGridEditingUnit.Row, true);      // commit řádku (uzavře editaci)

        if (!_db.ChangeTracker.HasChanges())                          // EF Core eviduje nějaké změny?
        {
            MessageBox.Show("Žádné změny k uložení.", "Informace",    // nic neměnit → info a konec
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _db.SaveChanges();                                            // trvalé uložení změn do DB
        MessageBox.Show("Změny byly uloženy.", "Hotovo",              // potvrzení uživateli
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
    catch (System.Exception ex)                                       // chyba při ukládání
    {
        MessageBox.Show("Nepodařilo se uložit změny.\n\n" + ex, "Chyba",
            MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e) // smazat vybraný řádek
{
    var selected = StudentsGrid.SelectedItem as Student;               // zjistit, co je vybráno
    if (selected == null)                                              // nic vybráno?
    {
        MessageBox.Show("Nejprve vyberte studenta v tabulce.", "Upozornění",
            MessageBoxButton.OK, MessageBoxImage.Information);
        return;
    }

    var answer = MessageBox.Show("Opravdu smazat vybraného studenta?", // potvrzení akce
        "Potvrzení", MessageBoxButton.YesNo, MessageBoxImage.Question);

    if (answer != MessageBoxResult.Yes)                                // pokud uživatel nedal Ano → konec
        return;

    _db.Students.Remove(selected);                                     // smazat z kontextu
    try
    {
        _db.SaveChanges();                                             // potvrdit v DB
        _students.Remove(selected);                                    // odstranit i z UI kolekce
    }
    catch (System.Exception ex)                                        // případná chyba smazání
    {
        MessageBox.Show("Smazání se nepodařilo.\n\n" + ex, "Chyba",
            MessageBoxButton.OK, MessageBoxImage.Error);
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
<Window xmlns:sys="clr-namespace:System;assembly=System.Runtime">  <!-- import System.Int32 pro XAML -->
  <DataGrid.Columns>                                               <!-- sloupce DataGridu -->
    <DataGridComboBoxColumn Header="Ročník"                        <!-- sloupec jako ComboBox -->
        SelectedItemBinding="{Binding Year, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"> <!-- bind na int Year -->
      <DataGridComboBoxColumn.ItemsSource>                         <!-- zdroj položek pro výběr -->
        <x:Array Type="{x:Type sys:Int32}">                         <!-- pole čísel 1..6 -->
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

> V code‑behind můžeš číst vybranou hodnotu pohodlněji přes `if (TxtYear.SelectedItem is int y) { ... }`.  
> Pokud zůstane TextBox, je potřeba hlídat převod na číslo — právě proto je **ComboBox** nejjednodušší bezpečné řešení.

---

## Jak to celé navazuje
- **Model (`Student`)**: popisuje sloupce a základní pravidla (DataAnnotations).  
- **Kontext (`StudentContext`)**: drží připojení a obsluhuje práci s DB, včetně prvního naplnění (seed).  
- **UI (`MainWindow.xaml`)**: DataGrid pro zobrazení a editaci, formulářové prvky pro přidání.  
- **Code‑behind (`MainWindow.xaml.cs`)**: načtení dat do `ObservableCollection`, napojení na DataGrid a obsluha tlačítek (přidat/uložit/smazat).  
- **PropertyChanged.Fody**: postará se o automatické notifikace změn do UI.

---

## Spuštění
1. **Build** (Ctrl+Shift+B).  
2. **Start** (F5).  
3. Při prvním spuštění se vytvoří LocalDB databáze a v mřížce uvidíš **seedovaná** data.  
4. Přidávej nové studenty formulářem. Upravuj buňky přímo v mřížce. Ulož změny tlačítkem **💾 Uložit**.  
5. Smaž vybraný řádek tlačítkem **Smazat vybraného**.

---

## Tipy k potížím
- Změnil/a jsem strukturu tabulky a něco nesedí → v **SQL Server Object Explorer** smaž LocalDB databázi, nebo změň `Initial Catalog` v connection stringu na nové jméno.  
- Fody hlásí chybějící konfiguraci → přidej `FodyWeavers.xml` (viz výše).  
- DataGrid je prázdný → zkontroluj `StudentsGrid.ItemsSource = _studentsView` a že `_students` plníš načtenými daty.

---

## Shrnutí (verze bez komentářů)

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

### `MainWindow.xaml.cs` (klíčové části pro Úkoly 1–3)

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
Tento výukový materiál můžeš volně používat v rámci výuky. Pokud ti pomohl, přidej ⭐ na GitHubu.
