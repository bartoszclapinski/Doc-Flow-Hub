# Uproszczony stos technologiczny systemu zarządzania dokumentami

## 1. Frontend

### ASP.NET Core MVC z Razor Pages
- **ASP.NET Core MVC** - tradycyjne podejście serwerowe z generowaniem widoków HTML
- **Razor Pages** - prosty model programowania dla stron internetowych
- **Bootstrap 5** - popularna biblioteka CSS dla responsywnego interfejsu
- Minimalne wykorzystanie JavaScript (tylko gdzie niezbędne)
- Gotowe komponenty:
  - Formularze z walidacją po stronie klienta
  - Proste tabele z sortowaniem
  - Podstawowe modalne okna dialogowe

## 2. Backend

### ASP.NET Core 8 MVC + Entity Framework Core 8
- **ASP.NET Core 8** z prostą architekturą:
  - Kontrolery i widoki (MVC)
  - Podstawowe serwisy dla logiki biznesowej
  - Repozytoria dla dostępu do danych (opcjonalnie)
  
- **Proste przekazywanie danych** zamiast zaawansowanego mapowania:
  - Bezpośrednie używanie ViewModeli
  - Podstawowe metody konwersji między modelami

- **Entity Framework Core 8** z code-first approach
- **SQL Server Express LocalDB** (dla developmentu)
- **ASP.NET Core Identity** do zarządzania użytkownikami i uprawnieniami

## 3. Przechowywanie i zarządzanie dokumentami

- **System plików serwera**:
  - Przechowywanie dokumentów w folderach na serwerze
  - Prosta struktura folderów według kategorii/użytkowników
  
- **Podstawowa obsługa wersjonowania**:
  - Zapisywanie każdej wersji jako osobnego pliku
  - Metadane dokumentów w bazie danych SQL
  - Nazewnictwo plików oparte na timestampach

## 4. Komunikacja z AI

### Bezpośrednia integracja z OpenAI
- **Pojedynczy dostawca AI** (OpenAI) bez złożonej abstrakcji
- **HttpClient** do komunikacji z API
- **Proste cachowanie odpowiedzi** dla powtarzalnych zapytań
- Podstawowy panel do wprowadzania klucza API

## 5. Testowanie

### Podstawowe testy jednostkowe
- **xUnit** - nowoczesny framework do testów jednostkowych w .NET
- **Moq** - popularna biblioteka do mockowania
- Skupienie na testach najważniejszych funkcjonalności biznesowych
- Pominięcie złożonych testów integracyjnych na początkowym etapie

## 6. CI/CD i Hosting

### GitHub Actions + Azure App Service
- **GitHub Actions**:
  - Prosty workflow do uruchamiania testów
  - Podstawowy proces budowania aplikacji
  - Deployment do Azure App Service

- **Azure App Service**:
  - Free/Shared tier dla hostingu (wystarczający dla MVP)
  - Wbudowane zarządzanie certyfikatami SSL
  - Łatwy deployment bezpośrednio z Visual Studio lub GitHub

## 7. Monitoring i bezpieczeństwo

- **Podstawowe logowanie**:
  - Wykorzystanie wbudowanego systemu logowania ASP.NET
  - Zapisywanie logów do pliku tekstowego

- **Bezpieczeństwo**:
  - HTTPS z certyfikatami dostarczanymi przez Azure
  - ASP.NET Core Identity z domyślnymi ustawieniami
  - Podstawowa walidacja danych wejściowych

## 8. Narzędzia deweloperskie

- **Visual Studio 2022 Community Edition** jako główne IDE
- **SQL Server Management Studio Express** do zarządzania bazą danych
- **GitHub Desktop** dla łatwiejszego zarządzania repozytorium
- **dotnet-ef** CLI do zarządzania migracjami (przez konsolę Package Manager w VS)

## 9. Narzędzia do współpracy

- **GitHub** do przechowywania kodu i zarządzania projektami
- **GitHub Issues** do śledzenia zadań i błędów
- **GitHub Wiki** do podstawowej dokumentacji

## 10. Środowiska

1. **Lokalne środowisko deweloperskie**:
   - LocalDB SQL Server Express
   - Lokalny system plików dla dokumentów
   - Konfiguracja przez appsettings.Development.json

2. **Środowisko produkcyjne**:
   - Azure App Service (Free/Shared tier)
   - Azure SQL Database (Basic tier)
   - System plików aplikacji dla dokumentów
   
## Ścieżka rozwoju

W miarę zdobywania doświadczenia można rozbudować system o:

1. Migrację do Blazor WebAssembly dla lepszego UX
2. Konteneryzację z Docker
3. Przejście na MinIO dla lepszego zarządzania dokumentami
4. Rozbudowaną abstrakcję AI z wieloma dostawcami
5. Zaawansowane testowanie i monitoring 