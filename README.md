# WPF (.NET 9) + EF Core: Students CRUD — výukový projekt

Tento **výukový README** krok‑za‑krokem ukazuje, jak ve Visual Studiu 2022 vytvořit **WPF Application (.NET)** s databází přes **Entity Framework Core (SQL Server LocalDB)**. Projekt umí:

1) **Vypsat** tabulku studentů do `DataGridu`  
2) **Přidávat** nové záznamy z formuláře (bez validace)  
3) **Upravovat** a **mazat** záznamy přímo v mřížce + **Uložit** změny do DB (s potvrzením)  
4) **Zabránit chybnému vstupu** u „Ročník“ tím, že povolíme **pouze výběr 1–6** z rozbalovacího seznamu

> Vše je **klikací** ve VS 2022 (žádný terminál). Šablona v českém VS: **„Aplikace WPF“** / v anglickém **„WPF Application“** pro **.NET** (neplést se starou **„WPF App (.NET Framework)“**).  
> README vychází z tvých souborů `StudentContext.cs`, `Student.cs` a `MainWindow.xaml.cs`. (Kód kontextu a modelu viz zdrojáky; `MainWindow.xaml.cs` je přizpůsoben dle finální verze z konverzace.)

---

## 0) Příprava projektu (klikací postup)

### 0.1 Vytvoření projektu

1. Otevři **Visual Studio 2022**.  
2. **Create a new project** → vyber **WPF Application** (pro .NET) → **Next**.  
3. **Project name** zadej např. `WPF_Aplikace_s_db`.  
4. **Framework** nastav na **.NET 9.0** → **Create**.

### 0.2 Instalace NuGet balíčků

V **Solution Exploreru** klikni pravým na projekt → **Manage NuGet Packages…** → záložka **Browse** a nainstaluj:

- **Microsoft.EntityFrameworkCore.SqlServer** – poskytovatel EF Core pro SQL Server (včetně **LocalDB**).  
- **Microsoft.EntityFrameworkCore.Tools** – design‑time nástroje (užitečné do budoucna).  
- **PropertyChanged.Fody** *(přidá i balíček **Fody**)* – přidá automaticky `INotifyPropertyChanged` přes atribut `[AddINotifyPropertyChangedInterface]`.

### 0.3 Fody konfigurace (jen pokud si projekt vyžádá)
Vytvoř soubor **`FodyWeavers.xml`**:

```xml
<Weavers>
  <PropertyChanged />
</Weavers>
```

> **Databáze**: používáme SQL Server **LocalDB**. Při prvním spuštění se DB vytvoří a naplní vzorovými daty. Kdyby bylo třeba „čisté“ prostředí po změně schématu, ve VS v **SQL Server Object Explorer** databázi smaž, nebo v connection stringu změň `Initial Catalog` na jiné jméno.

---

# Učební postup po úkolech
Každý krok ukazujeme **nejdřív bez komentářů** a hned **poté s komentáři každého řádku**.  
Pozor: Do **Úkolu 3** necháváme `DataGrid` **jen pro čtení** (IsReadOnly="True") a **nepoužíváme** `Mode=TwoWay` ani `UpdateSourceTrigger`. Teprve v Úkolu 3 povolíme editaci.

---

## Úkol 1 — Vypsat data ze `StudentContext` do `DataGridu` (čtení)

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
using PropertyChanged;                             // weaver: automaticky přidá INotifyPropertyChanged (Fody)
using System;                                      // základní typy (DateTime)
using System.ComponentModel.DataAnnotations;       // DataAnnotations: Required, StringLength, Range
using System.ComponentModel.DataAnnotations.Schema;// mapování do DB: DatabaseGenerated

namespace WPF_Aplikace_s_db.Data                    // jmenný prostor dat
{
    [AddINotifyPropertyChangedInterface]            // Fody: UI se samo překreslí při změně vlastností
    public class Student                            // entita => řádek v tabulce Students
    {
        [Key]                                       // primární klíč
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Id generuje DB (IDENTITY)
        public int Id { get; set; }                 // unikátní číslo

        [Required]                                   // povinné
        [StringLength(30)]                           // max délka
        public string FirstName { get; set; } = string.Empty; // jméno

        [Required]
        [StringLength(30)]
        public string LastName { get; set; } = string.Empty;  // příjmení

        [Range(1, 6)]                                // povolený rozsah 1–6
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
using Microsoft.EntityFrameworkCore;        // EF Core: obsahuje DbContext, DbSet<T>, UseSqlServer(...) atd.
using System.Collections.Generic;           // List<T> pro vytvoření seznamu seed dat
using System.Linq;                          // LINQ: Any(), ToList(), OrderBy(...) apod.

namespace WPF_Aplikace_s_db.Data            // Datová vrstva aplikace (modely + kontext)
{
    // DĚDIČNOST:
    //  - "StudentContext : DbContext" znamená, že StudentContext PŘEBÍRÁ (dědí) vlastnosti a chování třídy DbContext.
    //  - DbContext je základní třída EF Core (z balíčku Microsoft.EntityFrameworkCore),
    //    která umí sledovat entity (tracking), navazovat připojení k DB a provádět CRUD operace.
    //  - Díky dědičnosti získáváme metody jako SaveChanges(), Set<T>(), Database.EnsureCreated() atd.
    public class StudentContext : DbContext
    {
        // DbSet = "virtuální tabulka". EF Core z toho odvodí tabulku Students.
        // Umožňuje provádět dotazy (LINQ) i CRUD nad entitou Student.
        public DbSet<Student> Students => Set<Student>();

        // Konfigurace připojení do DB.
        // Přepisujeme (override) metodu z DbContextu, abychom určili, JAKÝ poskytovatel databáze použít.
        // ZDE: SQL Server (LocalDB) přes UseSqlServer(connectionString).
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Opatrnost: pokud už je kontext nakonfigurovaný jinde (např. DI v ASP.NET), nesahej na to znovu.
            if (!optionsBuilder.IsConfigured)
            {
                // UseSqlServer je rozšiřující metoda z balíčku Microsoft.EntityFrameworkCore.SqlServer
                // a nastaví EF Core, aby používal SQL Server (včetně LocalDB) jako cíl databáze.
                optionsBuilder.UseSqlServer(
                    "Data Source=(localdb)\\MSSQLLocalDB;" +  // LocalDB = lokální vývojový SQL Server
                    "Initial Catalog=StudentDbDemo1;" +       // název databáze (vytvoří se automaticky)
                    "Integrated Security=True;" +             // přihlášení Windows účtem
                    "TrustServerCertificate=True"             // ignoruj ověření certifikátu (pro LocalDB v pohodě)
                );
            }
        }

        // Pomocná metoda, kterou voláme při startu aplikace:
        // 1) zajistí existenci DB (pokud chybí → vytvoří),
        // 2) naplní tabulku Students vzorovými daty (jen když je prázdná).
        public void EnsureCreatedAndSeed()
        {
            // Database.EnsureCreated():
            //  - Vytvoří databázi a schéma TABULEK podle modelu, pokud ještě neexistuje.
            //  - NENÍ to totéž co migrace (Migrations)! Pro jednoduché demo je ale ideální.
            Database.EnsureCreated();

            // Naplnění počátečními daty (seed), pokud je tabulka prázdná.
            SeedIfEmpty();
        }

        // Seed dat (jen jednorázově, když v DB nic není).
        public void SeedIfEmpty()
        {
            // LINQ Any(): zkontroluje, zda existuje alespoň jeden řádek.
            if (!Students.Any())
            {
                // Sestav vzorovou kolekci studentů
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

                // Přidej celou sadu do kontextu (pending changes)
                Students.AddRange(initial);

                // Persistuj změny do databáze: EF vygeneruje INSERTy a uloží je do SQL Serveru.
                SaveChanges();
            }
        }
    }
}

```

### 1C) `MainWindow.xaml` — DataGrid **jen pro čtení**

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
              IsReadOnly="True">
      <DataGrid.Columns>
        <DataGridTextColumn Header="ID"        Binding="{Binding Id}"        Width="70"/>
        <DataGridTextColumn Header="Jméno"     Binding="{Binding FirstName}" Width="*"/>
        <DataGridTextColumn Header="Příjmení"  Binding="{Binding LastName}"  Width="*"/>
        <DataGridTextColumn Header="Ročník"    Binding="{Binding Year}"      Width="90"/>
        <DataGridTextColumn Header="E‑mail"    Binding="{Binding Email}"     Width="2*"/>
        <DataGridTextColumn Header="Vytvořeno" Binding="{Binding CreatedAt, StringFormat={}{0:yyyy-MM-dd HH:mm:ss}}" Width="180"/>
      </DataGrid.Columns>
    </DataGrid>
  </Grid>
</Window>
```

**S komentáři (každý řádek):**

```xml
<!--
  DŮLEŽITÉ PRO VAZBY (Binding):
  - V code-behind nastavíš: StudentsGrid.ItemsSource = _studentsView;  (_studentsView je pohled nad ObservableCollection<Student>)
  - Každý řádek DataGridu má pak DataContext = JEDNA instance Student.
  - Binding v každém sloupci (např. {Binding FirstName}) čte/zapisuje přímo do vlastnosti té řádkové instance Student.FirstName.
-->

<Window
  x:Class="WPF_Aplikace_s_db.MainWindow"               <!-- Třída code-behind, která toto XAML obsluhuje -->
  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"  <!-- Jmenný prostor pro WPF prvky -->
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"             <!-- Jmenný prostor pro XAML rozšíření (x:Name apod.) -->
  Title="Students" Height="450" Width="800">                          <!-- Titulek okna a počáteční velikost -->

  <!-- Grid = hlavní rozvržení okna (jedna buňka s vnitřním okrajem) -->
  <Grid Margin="12">                                                  <!-- Vnitřní okraj obsahu okna -->

    <!-- DataGrid = tabulka, kde každý řádek reprezentuje jeden Student z ItemsSource -->
    <DataGrid
      x:Name="StudentsGrid"                                           <!-- Název prvku pro přístup v C# (StudentsGrid.ItemsSource = ...) -->
      AutoGenerateColumns="False"                                     <!-- Sloupce definujeme ručně (plná kontrola) -->
      CanUserAddRows="False"                                          <!-- Skryje prázdný „*“ řádek pro ruční přidávání -->
      IsReadOnly="False">                                             <!-- Povolit editaci buněk (TwoWay binding u textových sloupců) -->

      <!-- Ručně definované sloupce. Každý Binding míří na vlastnost TŘÍDY Student. -->
      <DataGrid.Columns>

        <!-- ID (primární klíč) -->
        <DataGridTextColumn
          Header="ID"                                                 <!-- Text v záhlaví sloupce -->
          Binding="{Binding Id}"                                      <!-- Čti/Zapiš Student.Id (číselná vlastnost) -->
          Width="70"/>                                                <!-- Pevná šířka 70 px -->

        <!-- Jméno (FirstName) -->
        <DataGridTextColumn
          Header="Jméno"
          Binding="{Binding FirstName}"                               <!-- Čti/Zapiš Student.FirstName. 
                                                                          Výchozí mód u DataGridTextColumn je TwoWay,
                                                                          takže po potvrzení editace se hodnota propíše zpět. -->
          Width="*"/>                                                 <!-- Hvězdičková šířka = adaptivně vyplní prostor -->

        <!-- Příjmení (LastName) -->
        <DataGridTextColumn
          Header="Příjmení"
          Binding="{Binding LastName}"                                <!-- Čti/Zapiš Student.LastName -->
          Width="*"/>

        <!-- Ročník (Year) -->
        <DataGridTextColumn
          Header="Ročník"
          Binding="{Binding Year}"                                    <!-- Čti/Zapiš Student.Year (int). 
                                                                          Později lze nahradit DataGridComboBoxColumn (1–6). -->
          Width="90"/>                                                <!-- Pevná šířka, aby číslo nebylo nalepené -->

        <!-- E-mail (Email) -->
        <DataGridTextColumn
          Header="E-mail"
          Binding="{Binding Email}"                                   <!-- Čti/Zapiš Student.Email (string) -->
          Width="2*"/>                                                <!-- Dvojnásobný podíl šířky oproti sloupcům se * -->

        <!-- Vytvořeno (CreatedAt) s formátováním -->
        <DataGridTextColumn
          Header="Vytvořeno"
          Binding="{Binding CreatedAt,                                 <!-- Čti Student.CreatedAt (DateTime) -->
                            StringFormat={}{0:yyyy-MM-dd HH:mm:ss}}"   <!-- Vykresli s .NET formátem; {} uniká XAML markup -->
          Width="180"/>                                               <!-- Pevná šířka pro konzistentní zobrazení data/času -->

      </DataGrid.Columns>
    </DataGrid>
  </Grid>
</Window>

```

### 1D) `MainWindow.xaml.cs` — načtení dat (čtení)

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
using System.Collections.ObjectModel;                 // ObservableCollection = kolekce pro UI
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

## Úkol 2 — Přidávání záznamu (stále čtení v mřížce)

### 2A) `MainWindow.xaml` — doplníme formulář dole + tlačítko

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
            IsReadOnly="True">
    <DataGrid.Columns>
      <DataGridTextColumn Header="ID"        Binding="{Binding Id}"        Width="70"/>
      <DataGridTextColumn Header="Jméno"     Binding="{Binding FirstName}" Width="*"/>
      <DataGridTextColumn Header="Příjmení"  Binding="{Binding LastName}"  Width="*"/>
      <DataGridTextColumn Header="Ročník"    Binding="{Binding Year}"      Width="90"/>
      <DataGridTextColumn Header="E‑mail"    Binding="{Binding Email}"     Width="2*"/>
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
<!-- Hlavní kontejner okna: Grid = mřížka pro rozložení prvků do řádků/sloupců -->
<Grid Margin="12">
  <!-- Definice řádků mřížky (shora dolů) -->
  <Grid.RowDefinitions>
    <!-- 1. řádek: výška * (hvězdičková) = vyplní dostupný prostor → DataGrid -->
    <RowDefinition Height="*"/>
    <!-- 2. řádek: výška Auto = podle obsahu → formulář pro přidání -->
    <RowDefinition Height="Auto"/>
  </Grid.RowDefinitions>

  <!-- DataGrid = tabulkový výpis studentů -->
  <DataGrid
      x:Name="StudentsGrid"            <!-- Jméno prvku pro přístup v code-behind (C#) -->
      Grid.Row="0"                     <!-- Umístění do 1. řádku mřížky (indexování od 0) -->
      AutoGenerateColumns="False"      <!-- Sloupce si definujeme ručně (viz níže) -->
      CanUserAddRows="False"           <!-- Skryje prázdný řádek „*“ pro ruční přidávání -->
      IsReadOnly="True">               <!-- V Úkolu 1–2 pouze čtení; editace až v Úkolu 3 -->

    <!-- Ručně definované sloupce DataGridu (pořadí = zleva doprava) -->
    <DataGrid.Columns>

      <!-- Sloupec ID (číselný primární klíč) -->
      <DataGridTextColumn
          Header="ID"                  <!-- Záhlaví sloupce v UI -->
          Binding="{Binding Id}"       <!-- Vazba na vlastnost 'Id' objektu Student -->
          Width="70"/>                 <!-- Pevná šířka 70 px -->

      <!-- Sloupec Jméno (FirstName) -->
      <DataGridTextColumn
          Header="Jméno"               <!-- Titulek hlavičky -->
          Binding="{Binding FirstName}"<!-- Jednosměrná vazba (do Úkolu 3 bez TwoWay) -->
          Width="*"/>                  <!-- Hvězdičková šířka = vyplní zbytek místa rovnoměrně -->

      <!-- Sloupec Příjmení (LastName) -->
      <DataGridTextColumn
          Header="Příjmení"
          Binding="{Binding LastName}"
          Width="*"/>

      <!-- Sloupec Ročník (Year) -->
      <DataGridTextColumn
          Header="Ročník"
          Binding="{Binding Year}"
          Width="90"/>                 <!-- Pevná šířka ~90 px, aby číslo nebylo „nalepené“ -->

      <!-- Sloupec E-mail (Email) -->
      <DataGridTextColumn
          Header="E-mail"
          Binding="{Binding Email}"
          Width="2*"/>                 <!-- Dvojnásobná hvězdičková šířka = širší než Jméno/Příjmení -->

      <!-- Sloupec Vytvořeno (CreatedAt) s formátováním data/času -->
      <DataGridTextColumn
          Header="Vytvořeno"
          Binding="{Binding CreatedAt,
                            StringFormat={}{0:yyyy-MM-dd HH:mm:ss}}"
          Width="180"/>
      <!-- Pozn.: StringFormat s prázdným prefixem {} potlačí interpretaci jako markup
           a aplikuje .NET formát {0:yyyy-MM-dd HH:mm:ss} přímo na DateTime -->

    </DataGrid.Columns>
  </DataGrid>

  <!-- Spodní formulář: horizontální panel s textovými poli a tlačítkem -->
  <StackPanel
      Grid.Row="1"                      <!-- Umístění do 2. řádku (Auto) -->
      Orientation="Horizontal"          <!-- Skládá vnitřní prvky zleva doprava -->
      Margin="0,10,0,0">                <!-- Vnější okraj: Left=0, Top=10, Right=0, Bottom=0 -->

    <!-- TextBox pro Jméno -->
    <TextBox
        x:Name="TxtFirstName"           <!-- Jméno pole pro čtení v C# (TxtFirstName.Text) -->
        Width="150"                     <!-- Pevná šířka vstupu -->
        Margin="0,0,8,0"/>              <!-- Pravý odstup 8 px od dalšího prvku -->

    <!-- TextBox pro Příjmení -->
    <TextBox
        x:Name="TxtLastName"
        Width="150"
        Margin="0,0,8,0"/>

    <!-- TextBox pro Ročník
         (v Úkolu 4 se typicky nahradí ComboBoxem 1–6, zde zatím bez omezení) -->
    <TextBox
        x:Name="TxtYear"
        Width="60"
        Margin="0,0,8,0"/>

    <!-- TextBox pro E-mail -->
    <TextBox
        x:Name="TxtEmail"
        Width="220"
        Margin="0,0,8,0"/>

    <!-- Tlačítko pro přidání nového záznamu
         - Click = název obslužné metody v code-behind (MainWindow.xaml.cs) -->
    <Button
        x:Name="BtnAddStudent"
        Content="Přidat studenta"       <!-- Text na tlačítku -->
        Click="BtnAddStudent_Click"/>   <!-- Event handler po kliku -->

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
private void BtnAddStudent_Click(object sender, RoutedEventArgs e)  // klik na "Přidat studenta"
{
    // Načtení hodnot z formuláře (bez validace):
    //  - '?? string.Empty' = null-coalescing: když je vlevo null, použije se prázdný řetězec "".
    //    Díky tomu následující .Trim() nikdy nepadne na NullReferenceException.
    //  - .Trim() vrátí nový řetězec bez bílých znaků na ZAČÁTKU i na KONCI (mezery, tabulátory, nové řádky).
    //    Neodstraňuje znaky uprostřed textu.
    string firstName = (TxtFirstName.Text ?? string.Empty).Trim();   // jméno: ochrana proti null + ořez okrajových mezer
    string lastName  = (TxtLastName.Text  ?? string.Empty).Trim();   // příjmení: totéž
    string email     = (TxtEmail.Text     ?? string.Empty).Trim();   // e-mail: totéž

    // Výchozí hodnota ročníku je 1. Pokud parsování selže, zůstane 1 (smysluplná defaultní hodnota).
    int year = 1;

    // Bezpečný převod textu na int:
    //  - (TxtYear.Text ?? string.Empty)          → ochrana proti null
    //  - .Trim()                                  → odstraní okrajové mezery (např. " 2 " → "2")
    //  - int.TryParse(..., out year)              → zkusí převést; když to nejde, nehodí výjimku a 'year' zůstane 1
    int.TryParse((TxtYear.Text ?? string.Empty).Trim(), out year);

    // Vytvoří objekt s novým objektem, který následně přidáváme do db.
    var s = new Student                                             // nová entita
    {
      // FirstName - Je název sloupce v tabulce, kterou jsme si definovali v Student.cs
      // firstName - Je proměnná, která bere data z XAML
        FirstName = firstName,
        LastName  = lastName,
        Year      = year,
        Email     = email
    };

    _db.Students.Add(s);                                            // přidej do kontextu
    _db.SaveChanges();                                              // ulož do DB (vygeneruje Id)

    _students.Add(s);                                               // doplň do UI kolekce
    StudentsGrid.SelectedItem = s;                                  // označ řádek
    StudentsGrid.ScrollIntoView(s);                                 // Vykresli do UI

    // Vymaže data v inputech, nastaví je string.Empty
    TxtFirstName.Text = string.Empty;
    TxtLastName.Text = string.Empty;
    TxtEmail.Text = string.Empty;
    TxtYear.Text = string.Empty; // vyčisti formulář
}
```

> **Operátor `??` (null‑coalescing):** `A ?? B` vrátí `A`, když **není null**, jinak vrátí `B`. Tím zajistíš, že `.Trim()` nepoběží na `null`.

---

## Úkol 3 — Upravovat v mřížce + Uložit/Mazat (s potvrzením)

Teď povolíme editaci v `DataGridu` a přidáme tlačítka **Uložit**/**Smazat** na **nový řádek Gridu** (řádek **3**).

### 3A) `MainWindow.xaml` — povolit editaci + rozšířit layout

**Bez komentářů:**

```xml
<Window x:Class="WPF_Aplikace_s_db.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Students" Height="520" Width="800">
  <Grid Margin="12">
    <Grid.RowDefinitions>
      <RowDefinition Height="2*"/>
      <RowDefinition Height="6"/>
      <RowDefinition Height="Auto"/>
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

    <GridSplitter Grid.Row="1" Height="6" HorizontalAlignment="Stretch" VerticalAlignment="Center" Background="#DDD"/>

    <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,10,0,0">
      <TextBox x:Name="TxtFirstName" Width="150" Margin="0,0,8,0"/>
      <TextBox x:Name="TxtLastName"  Width="150" Margin="0,0,8,0"/>
      <TextBox x:Name="TxtYear"      Width="60"  Margin="0,0,8,0"/>
      <TextBox x:Name="TxtEmail"     Width="220" Margin="0,0,8,0"/>
      <Button x:Name="BtnAddStudent" Content="Přidat studenta" Click="BtnAddStudent_Click"/>
    </StackPanel>

    <StackPanel Grid.Row="3" Orientation="Horizontal" Margin="0,10,0,0">
      <Button x:Name="BtnSave"           Content="💾 Uložit"          Click="BtnSave_Click"   Margin="0,0,8,0"/>
      <Button x:Name="BtnDeleteSelected" Content="Smazat vybraného"  Click="BtnDeleteSelected_Click"/>
    </StackPanel>
  </Grid>
</Window>
```

**S komentáři (každý řádek):**

```xml
<Window x:Class="WPF_Aplikace_s_db.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Students" Height="520" Width="800">
  <Grid Margin="12">
    <Grid.RowDefinitions>                                <!-- 4 řádky: grid, splitter, formulář, akce -->
      <RowDefinition Height="2*"/>                       <!-- 0: DataGrid -->
      <RowDefinition Height="6"/>                        <!-- 1: GridSplitter -->
      <RowDefinition Height="Auto"/>                     <!-- 2: formulář -->
      <RowDefinition Height="Auto"/>                     <!-- 3: Uložit/Smazat -->
    </Grid.RowDefinitions>

    <DataGrid x:Name="StudentsGrid" Grid.Row="0"
              AutoGenerateColumns="False"
              CanUserAddRows="False"
              IsReadOnly="False">                        <!-- Úkol 3: povolíme editaci -->
      <DataGrid.Columns>
        <DataGridTextColumn Header="ID"        Binding="{Binding Id}"        Width="70"/>
        <!-- Teď přidáváme dvoucestné svázání + okamžitý update -->
        <DataGridTextColumn Header="Jméno"     Binding="{Binding FirstName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
        <DataGridTextColumn Header="Příjmení"  Binding="{Binding LastName,  Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
        <DataGridTextColumn Header="Ročník"    Binding="{Binding Year,      Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="90"/>
        <DataGridTextColumn Header="E‑mail"    Binding="{Binding Email,     Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="2*"/>
        <DataGridTextColumn Header="Vytvořeno" Binding="{Binding CreatedAt, StringFormat={}{0:yyyy-MM-dd HH:mm:ss}}" Width="180"/>
      </DataGrid.Columns>
    </DataGrid>

    <GridSplitter Grid.Row="1" Height="6" HorizontalAlignment="Stretch" VerticalAlignment="Center" Background="#DDD"/>

    <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,10,0,0">
      <TextBox x:Name="TxtFirstName" Width="150" Margin="0,0,8,0"/>
      <TextBox x:Name="TxtLastName"  Width="150" Margin="0,0,8,0"/>
      <TextBox x:Name="TxtYear"      Width="60"  Margin="0,0,8,0"/>
      <TextBox x:Name="TxtEmail"     Width="220" Margin="0,0,8,0"/>
      <Button x:Name="BtnAddStudent" Content="Přidat studenta" Click="BtnAddStudent_Click"/>
    </StackPanel>

    <StackPanel Grid.Row="3" Orientation="Horizontal" Margin="0,10,0,0"> <!-- důležité: Grid.Row="3" -->
      <Button x:Name="BtnSave"           Content="💾 Uložit"          Click="BtnSave_Click"   Margin="0,0,8,0"/>
      <Button x:Name="BtnDeleteSelected" Content="Smazat vybraného"  Click="BtnDeleteSelected_Click"/>
    </StackPanel>
  </Grid>
</Window>
```

### 3B) `MainWindow.xaml.cs` — Uložit (s potvrzením)

**Bez komentářů:**

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
private void BtnSave_Click(object sender, RoutedEventArgs e)    // obsluha kliknutí na tlačítko "Uložit"
{
    // StudentsGrid.SelectedItem vrátí "aktuálně vybraný řádek" v DataGridu.
    // Protože DataGrid zobrazuje kolekci Student, očekáváme, že vybraný řádek bude instance Student.
    // Operátor "as" se POKUSÍ převést (cast) na Student:
    //  - když to jde → vrátí odkaz typu Student,
    //  - když to nejde nebo není nic vybráno → vrátí NULL (žádný objekt).
    var selected = StudentsGrid.SelectedItem as Student;        // vybraný Student NEBO null

    // Ověříme, že opravdu máme nějakého studenta vybraného.
    // (null znamená "nic", "žádný objekt", tedy není co ukládat/na co se ptát uživatele.)
    if (selected != null)                                       // pokračuj jen, když je něco vybráno
    {
        // Zobrazíme potvrzovací dialog, do textu doplníme jméno a příjmení vybraného studenta.
        // MessageBox.Show(ownerWindow, text, caption, buttons, icon)
        //  - this               → okno, které vlastní dialog (dobré pro modalitu a fokus)
        //  - string interpolace → $"... {selected.FirstName} {selected.LastName}?"
        //  - MessageBoxButton.YesNo → zobrazí tlačítka Ano / Ne
        //  - MessageBoxImage.Question → otazníková ikona
        var result = MessageBox.Show(this,
            $"Opravdu chcete údaje studenta {selected.FirstName} {selected.LastName}?",
            "Uložit?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        // Vyhodnotíme, co uživatel zvolil v dialogu.
        if (result == MessageBoxResult.Yes)                     // pokud odpověděl "Ano"
        {
            // Uložíme všechny dosud neuložené změny, které EF Core sleduje v DbContextu (_db).
            // EF zjistí, které entity mají změněný stav (Modified/Added/Deleted) a vygeneruje
            // odpovídající SQL příkazy (UPDATE/INSERT/DELETE) do databáze.
            _db.SaveChanges();
        }
    }
    // Když selected == null (nic nevybráno), funkce "tiše" skončí bez dotazu/ukládání.
}

```

> **Tip (volitelné):** Když chceš mít jistotu, že DataGrid zrovna rozeditované buňky commitnul, můžeš před uložením volat:
> ```csharp
> StudentsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
> StudentsGrid.CommitEdit(DataGridEditingUnit.Row, true);
> ```

### 3C) `MainWindow.xaml.cs` — Smazat (s potvrzením)

**Bez komentářů:**

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
private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e) // obsluha kliknutí na "Smazat vybraného"
{
    // StudentsGrid.SelectedItem → vrátí aktuálně vybraný řádek v DataGridu.
    // Každý řádek reprezentuje jednu instanci Student (z ItemsSource).
    // Operátor "as" se POKUSÍ přetypovat na Student:
    //   - když je něco vybráno a typ sedí → dostaneme referenci na Student,
    //   - když není nic vybráno / typ nesedí → dostaneme null (žádný objekt).
    var selected = StudentsGrid.SelectedItem as Student;   // vybraný Student nebo null

    // Pokud není vybrán žádný řádek (selected == null), nemá smysl pokračovat.
    if (selected != null)
    {
        // Potvrzovací dialog před destruktivní akcí (smazání).
        //  - this = vlastník okna (správná modalita a fokus)
        //  - interpolace $"... {selected.FirstName} {selected.LastName}" doplní jméno studenta do textu
        //  - MessageBoxButton.YesNo = zobrazí tlačítka Ano/Ne
        //  - MessageBoxImage.Question = otazníková ikona
        var result = MessageBox.Show(this,
            $"Opravdu smazat studenta {selected.FirstName} {selected.LastName}?",
            "Smazat studenta",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        // Reagujeme jen na volbu Ano (Yes).
        if (result == MessageBoxResult.Yes)
        {
            // 1) Označíme entitu pro smazání v EF kontextu.
            //    - EF Core nastaví stav entity na Deleted (Entry.State = Deleted).
            //    - Smazání se zatím NEprovede v DB; je to jen příprava operace.
            _db.Students.Remove(selected);

            // 2) Persistujeme změny do databáze:
            //    - EF vygeneruje DELETE SQL podle primárního klíče (Id) a provede ho proti DB.
            //    - Pokud má tabulka cizí klíče bez kaskády, může DB smazání odmítnout (FK violation).
            _db.SaveChanges();

            // 3) Aktualizujeme UI kolekci (ObservableCollection<Student> _students),
            //    aby DataGrid okamžitě odrážel změnu i v paměti (zmizí řádek bez nutnosti reloadu).
            _students.Remove(selected);
        }
    }
}

```

---

## Úkol 4 — Zabránit chybnému vstupu pro „Ročník“ (omezit na 1–6)

Necháme uživatele **vybrat** hodnotu místo psaní. V mřížce použijeme `DataGridComboBoxColumn`, ve formuláři `ComboBox`.

**XAML (doplnění do Úkolu 3):**

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

  <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,10,0,0">
    <ComboBox x:Name="TxtYear" Width="60" IsEditable="False" SelectedIndex="-1" ToolTip="Vyberte ročník 1–6">
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
    <!-- ostatní prvky formuláře beze změny -->
  </StackPanel>
</Window>
```

**S komentáři (každý řádek):**

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

  <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,10,0,0">
    <ComboBox x:Name="TxtYear" Width="60" IsEditable="False" SelectedIndex="-1" ToolTip="Vyberte ročník 1–6">
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
    <!-- ostatní prvky formuláře beze změny -->
  </StackPanel>
</Window>
```

> V code‑behind můžeš dál používat `int.TryParse(TxtYear.Text, out year)` (Text bude naplněný vybraným číslem), nebo přistoupit přímo k hodnotě:
> ```csharp
> if (TxtYear.SelectedItem is int y) year = y;
> ```

---

## Jak to celé zapadá
- **Model (`Student`)** definuje sloupce a pravidla (DataAnnotations, Fody).  
- **Kontext (`StudentContext`)** nastaví LocalDB, vytvoří DB a naplní data.  
- **UI (`MainWindow.xaml`)**: v Úkolu 1–2 pouze čteme (IsReadOnly="True"); v Úkolu 3 povolíme editaci a přidáme Uložit/Smazat; v Úkolu 4 omezíme „Ročník“.  
- **Code‑behind (`MainWindow.xaml.cs`)**: načtení do `ObservableCollection`, přidání nové položky, uložení/mazání s potvrzením.

---

# Přílohy: kompletní minimální kód (bez komentářů)

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

### `MainWindow.xaml.cs` (části důležité pro Úkoly 1–3)
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

# Přehled použitého C# API („příkazů“)

## Základní C# / .NET
- `using ...;` – import jmenných prostorů (zpřístupnění typů bez plných názvů).  
- `namespace ... { ... }` – jmenný prostor pro logické seskupení kódu.  
- `class MainWindow : Window` – definice třídy dědící z `Window` (WPF okno).  
- `partial` – část třídy je doplněna generovaným kódem z XAML.  
- `var` – implicitní typování (překladač odvodí typ z pravé strany).  
- `string`, `int`, `DateTime` – základní datové typy.  
- `??` (null‑coalescing) – `A ?? B` vrátí `A` pokud není `null`, jinak `B`.  
- `int.TryParse(string, out int)` – bezpečný převod řetězce na číslo.  
- `ObservableCollection<T>` – kolekce, která oznamuje změny UI (přidání/odebrání).  
- `ICollectionView`, `CollectionViewSource.GetDefaultView` – obálka nad kolekcí (třídění, filtrování).  
- `MessageBox.Show(...)` – zobrazení dialogu (potvrzení, informace).  
- Linq: `OrderBy`, `ToList`, `Any` – práce s kolekcemi.

## Z NuGet balíčků (EF Core + Fody)
**Entity Framework Core (Microsoft.EntityFrameworkCore + .SqlServer):**
- `DbContext`, `DbSet<T>` – základ práce s DB (kontext a tabulky).  
- `OnConfiguring(DbContextOptionsBuilder)` – konfigurace připojení.  
- `UseSqlServer(connectionString)` – nastavení SQL Server/LocalDB providera.  
- `Database.EnsureCreated()` – vytvoří DB pokud ještě není.  
- `SaveChanges()` – uloží změny (INSERT/UPDATE/DELETE).  
- `Add`, `AddRange`, `Remove` – práce s entitami v kontextu.

**PropertyChanged.Fody (Fody + PropertyChanged.Fody):**
- `[AddINotifyPropertyChangedInterface]` – atribut, který weaverem doplní implementaci `INotifyPropertyChanged` do celé třídy. UI se díky tomu aktualizuje při změně vlastností bez ručního kódu.

---

# Přehled použitého XAML (tagy + klíčové atributy)

- `<Window ...>` – hlavní okno.  
  - `x:Class` – třída code‑behind (C#).  
  - `xmlns`, `xmlns:x` – XML jmenné prostory (WPF, XAML rozšíření).  
  - `Title`, `Height`, `Width` – základní parametry okna.

- `<Grid ...>` – kontejner s mřížkou.  
  - `Margin` – vnitřní okraj.  
  - `<Grid.RowDefinitions>` + `<RowDefinition Height="..."/>` – definice řádků (v Úkolu 3: 4 řádky).  
  - `Grid.Row="N"` – umístění prvku do konkrétního řádku.

- `<GridSplitter ...>` – táhlo pro změnu velikosti řádků (Úkol 3, řádek 1).  
  - `Height`, `HorizontalAlignment`, `VerticalAlignment`, `Background` – vzhled a rozměr.

- `<DataGrid ...>` – tabulkový výpis studentů.  
  - `x:Name` – jmenný identifikátor pro code‑behind.  
  - `AutoGenerateColumns="False"` – sloupce definujeme ručně.  
  - `CanUserAddRows="False"` – skryje prázdný řádek pro přidání.  
  - `IsReadOnly="True|False"` – celé UI buď jen ke čtení (Úkol 1–2), nebo editovatelné (Úkol 3).

- `<DataGridTextColumn ...>` – textový sloupec.  
  - `Header` – titulek sloupce.  
  - `Binding="{Binding Vlastnost}"` – mapování na vlastnost entity.  
  - `Width` – šířka (`70`, `*`, `2*` apod.).  
  - **Až v Úkolu 3:** `Mode=TwoWay, UpdateSourceTrigger=PropertyChanged` – dvoucestné svázání a okamžité promítnutí změny.

- `<DataGridComboBoxColumn ...>` – sloupec s výběrem hodnot (Úkol 4 pro „Ročník“).  
  - `SelectedItemBinding="{Binding Year, ...}"` – binduje vybranou hodnotu do `Year`.  
  - `<DataGridComboBoxColumn.ItemsSource>` + `<x:Array ...>` – pevný seznam hodnot 1–6.

- `<StackPanel ...>` – vodorovné seskupení prvků.  
  - `Orientation="Horizontal"`, `Margin="..."`, `Grid.Row="..."` – rozložení.  
  - (Úkol 3) **Pozor na `Grid.Row="3"`** pro panel s tlačítky Uložit/Smazat.

- `<TextBox ...>` – vstupní políčko (jméno/příjmení/ročník/e‑mail).  
  - `x:Name`, `Width`, `Margin` – identifikace a rozměry.

- `<ComboBox ...>` – výběr ročníku (Úkol 4).  
  - `IsEditable="False"` – nepsat, jen vybírat.  
  - `SelectedIndex="-1"` – žádná předvybraná položka.  
  - `<ComboBox.ItemsSource>` + `<x:Array ...>` – seznam 1–6.

- `<Button ...>` – tlačítka.  
  - `x:Name`, `Content`, `Click="Handler"` – identita, popisek a obsluha kliknutí.  
  - `Margin` – vnější okraj.

- `Binding` s `StringFormat` – formátování zobrazení (datum/čas).

---

## Spuštění
1. **Build** (Ctrl+Shift+B).  
2. **Start** (F5).  
3. Při prvním spuštění se vytvoří LocalDB a uvidíš **seedovaná data**.  
4. Úkol 2: přidávej nové studenty formulářem.  
5. Úkol 3: zapni editaci v mřížce, uprav, **Uložit** (s potvrzením), **Smazat** (s potvrzením).  
6. Úkol 4: používej výběr „Ročník“ (1–6) v mřížce i ve formuláři.

---

## Licence
Tento výukový materiál můžeš volně používat v rámci výuky. Pokud ti pomohl, přidej ⭐ na GitHubu.
