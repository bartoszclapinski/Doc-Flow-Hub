# Dokument wymagań produktu (PRD) - System Zarządzania Dokumentami

## 1. Przegląd produktu

System Zarządzania Dokumentami to aplikacja webowa zbudowana w technologii C# ASP.NET Core 8, umożliwiająca użytkownikom przechowywanie, organizowanie i wersjonowanie dokumentów w jednym centralnym miejscu. System został zaprojektowany z myślą o użytkownikach indywidualnych, którzy potrzebują narzędzia do efektywnego zarządzania swoimi dokumentami, z możliwością rozszerzenia funkcjonalności dla małych firm w przyszłości.

Kluczowe funkcje systemu obejmują:
- Zarządzanie dokumentami (dodawanie, aktualizacja, usuwanie)
- Zaawansowany system wersjonowania dokumentów
- Kategoryzację i organizację dokumentów
- Wyszukiwanie i filtrowanie według metadanych
- System uwierzytelniania i autoryzacji użytkowników
- Zarządzanie zespołami i współpraca w ramach zespołów

Produkt jest tworząc z wykorzystaniem następującego stosu technologicznego:
- Backend: ASP.NET Core 8 MVC/Razor Pages
- Frontend: Bootstrap z minimalnym wykorzystaniem JavaScript
- Baza danych: SQL Server (możliwe użycie wersji Express w środowisku deweloperskim)
- Uwierzytelnianie: ASP.NET Core Identity
- ORM: Entity Framework Core
- Testy: xUnit
- CI/CD: GitHub Actions

## 2. Problem użytkownika

Użytkownicy często borykają się z następującymi problemami podczas zarządzania swoimi dokumentami:

1. Rozproszenie dokumentów w różnych lokalizacjach, co utrudnia ich odnalezienie
2. Zapominanie o istnieniu wcześniej utworzonych dokumentów, co prowadzi do wielokrotnego pobierania tych samych plików
3. Przypadkowe nadpisywanie lub usuwanie ważnych informacji
4. Wielokrotne nanoszenie tych samych zmian w dokumentach
5. Trudności w śledzeniu historii zmian w dokumentach
6. Brak centralnego miejsca do organizacji i kategoryzacji dokumentów
7. Utrudniona współpraca w zespole przy zarządzaniu wspólnymi dokumentami

System Zarządzania Dokumentami rozwiązuje te problemy poprzez:
- Zapewnienie centralnego repozytorium dla wszystkich dokumentów
- Automatyczne śledzenie zmian i wersjonowanie dokumentów
- Funkcje kategoryzacji i wyszukiwania ułatwiające organizację
- Możliwość przywracania poprzednich wersji dokumentów
- Blokowanie dokumentów podczas edycji, zapobiegające konfliktom
- Zarządzanie zespołami umożliwiające współpracę przy dokumentach

## 3. Wymagania funkcjonalne

### 3.1. Zarządzanie użytkownikami
- Rejestracja i logowanie użytkowników
- Role użytkowników (administrator, standardowy użytkownik)
- Zarządzanie profilem użytkownika (aktualizacja danych, zmiana hasła)
- Odzyskiwanie hasła
- Tworzenie zespołów
- Dodawanie użytkowników do zespołów
- Usuwanie użytkowników z zespołów
- Przypisywanie ról w zespole (właściciel zespołu, członek)
- Zarządzanie uprawnieniami w ramach zespołu

### 3.2. Zarządzanie dokumentami
- Dodawanie dokumentów (obsługiwane formaty: markdown, doc, pdf, obrazy)
- Edycja metadanych dokumentów (tytuł, opis)
- Usuwanie dokumentów
- Limit rozmiaru pliku: 30 MB
- Brak wsparcia dla plików wideo
- Grupowanie dokumentów w projekty/foldery
- Kategoryzacja dokumentów z hierarchią dwupoziomową (kategoria główna + podkategoria/podkategorie)
- System automatycznie sugeruje kategorie na podstawie zawartości/nazwy dokumentu
- Współdzielenie dokumentów w ramach zespołu

### 3.3. System wersjonowania
- Automatyczne tworzenie nowej wersji przy każdej zmianie dokumentu
- Przechowywanie wszystkich wersji dokumentu do momentu jego usunięcia
- Historia wersji z listą wszystkich poprzednich wersji
- Podsumowanie oryginalnego dokumentu generowane przez AI przy pierwszym dodaniu
- Generowanie różnic między kolejnymi wersjami przez AI
- Możliwość przywrócenia dowolnej poprzedniej wersji dokumentu
- Blokowanie dokumentu podczas edycji przez jednego użytkownika

### 3.4. Wyszukiwanie i filtrowanie
- Wyszukiwanie według metadanych (nazwa, autor, kategoria, data modyfikacji, rozmiar)
- Podstawowe filtry dla listy dokumentów
- Brak wyszukiwania pełnotekstowego w treści dokumentów
- Filtrowanie dokumentów według zespołu

### 3.5. Interfejs użytkownika
- Dashboard z podsumowaniem kolekcji dokumentów użytkownika
- Widok "Ostatnio używane dokumenty" na stronie głównej
- Lista dokumentów z możliwością sortowania i podstawowego filtrowania
- Widok szczegółów dokumentu wraz z historią wersji
- Panel administracyjny dla zarządzania użytkownikami (tylko dla administratorów)
- Podstawowe powiadomienia o zmianach w dokumentach
- Interfejs zarządzania zespołami
- Widok dokumentów w ramach zespołu

## 4. Granice produktu

System Zarządzania Dokumentami w wersji MVP nie będzie obejmował następujących funkcjonalności:

1. Zaawansowane wyszukiwanie i filtrowanie
   - Brak wyszukiwania pełnotekstowego w zawartości dokumentów
   - Brak zaawansowanych filtrów kombinowanych

2. System tagów
   - W MVP dokumenty można kategoryzować, ale nie tagować

3. Zaawansowane funkcje współpracy
   - Brak edycji dokumentów w czasie rzeczywistym przez wielu użytkowników
   - Brak zaawansowanego zarządzania uprawnieniami do poszczególnych dokumentów

4. Zaawansowane funkcje wersjonowania
   - Brak wizualnego porównywania wersji dokumentów (diff)
   - Brak możliwości scalania wersji

5. Funkcje społecznościowe
   - Brak komentarzy i adnotacji do dokumentów
   - Brak funkcji współpracy w czasie rzeczywistym

6. Zaawansowane powiadomienia
   - Tylko podstawowe powiadomienia o zmianach

7. Integracje zewnętrzne
   - Brak API dla zewnętrznych systemów
   - Brak integracji z usługami chmurowymi

8. Funkcje mobilne
   - Brak dedykowanego interfejsu mobilnego
   - Brak aplikacji mobilnej

9. Funkcje eksportu/importu
   - Brak eksportu listy dokumentów
   - Brak masowego importu dokumentów

10. Podgląd dokumentów
    - Brak podglądu dokumentów bezpośrednio w przeglądarce
    - Dokumenty muszą być pobrane, aby je otworzyć

## 5. Historyjki użytkowników

### Zarządzanie kontem

#### US-001: Rejestracja nowego użytkownika
- Tytuł: Rejestracja nowego użytkownika
- Opis: Jako nowy użytkownik, chcę utworzyć konto w systemie, aby móc korzystać z funkcjonalności zarządzania dokumentami.
- Kryteria akceptacji:
  1. Użytkownik może wypełnić formularz rejestracji z polami: adres email, hasło, potwierdzenie hasła
  2. System waliduje poprawność adresu email i złożoność hasła
  3. System nie pozwala na rejestrację z już istniejącym adresem email
  4. Po rejestracji użytkownik otrzymuje powiadomienie o pomyślnym utworzeniu konta
  5. Po rejestracji użytkownik jest automatycznie logowany do systemu

#### US-002: Logowanie użytkownika
- Tytuł: Logowanie do systemu
- Opis: Jako zarejestrowany użytkownik, chcę zalogować się do systemu, aby uzyskać dostęp do moich dokumentów.
- Kryteria akceptacji:
  1. Użytkownik może wprowadzić swój adres email i hasło
  2. System weryfikuje poprawność danych logowania
  3. W przypadku błędnych danych, system wyświetla odpowiedni komunikat
  4. Po pomyślnym logowaniu, użytkownik jest przekierowany na stronę główną systemu
  5. System oferuje opcję "zapamiętaj mnie"

#### US-003: Resetowanie hasła
- Tytuł: Resetowanie zapomnianego hasła
- Opis: Jako użytkownik, który zapomniał hasła, chcę zresetować swoje hasło, aby odzyskać dostęp do konta.
- Kryteria akceptacji:
  1. Użytkownik może zażądać resetowania hasła podając swój adres email
  2. System wysyła link do resetowania hasła na podany adres email
  3. Link do resetowania hasła jest ważny przez ograniczony czas
  4. Użytkownik może ustawić nowe hasło po kliknięciu w link
  5. System potwierdza zmianę hasła i umożliwia logowanie z nowym hasłem

#### US-004: Zarządzanie profilem użytkownika
- Tytuł: Edycja danych profilu
- Opis: Jako zalogowany użytkownik, chcę edytować dane mojego profilu, aby aktualizować moje informacje.
- Kryteria akceptacji:
  1. Użytkownik może zmienić podstawowe dane profilu (imię, nazwisko)
  2. Użytkownik może zmienić hasło (wymagane podanie obecnego hasła)
  3. System waliduje wprowadzane dane
  4. System potwierdza zapisanie zmian w profilu

### Zarządzanie zespołami

#### US-005: Tworzenie zespołu
- Tytuł: Tworzenie nowego zespołu
- Opis: Jako zalogowany użytkownik, chcę utworzyć nowy zespół, aby umożliwić współpracę przy dokumentach z innymi użytkownikami.
- Kryteria akceptacji:
  1. Użytkownik może utworzyć nowy zespół podając jego nazwę i opcjonalny opis
  2. Użytkownik automatycznie staje się właścicielem utworzonego zespołu
  3. System waliduje unikalność nazwy zespołu
  4. System potwierdza utworzenie zespołu
  5. Nowy zespół jest widoczny na liście zespołów użytkownika

#### US-006: Dodawanie użytkowników do zespołu
- Tytuł: Dodawanie członków zespołu
- Opis: Jako właściciel zespołu, chcę dodawać innych użytkowników do mojego zespołu, aby umożliwić im dostęp do dokumentów zespołu.
- Kryteria akceptacji:
  1. Właściciel zespołu może wyszukiwać użytkowników po adresie email
  2. System weryfikuje istnienie użytkownika w systemie
  3. Właściciel może wysłać zaproszenie do zespołu
  4. Zaproszony użytkownik otrzymuje powiadomienie o zaproszeniu
  5. Po zaakceptowaniu zaproszenia, użytkownik staje się członkiem zespołu
  6. System potwierdza dodanie użytkownika do zespołu

#### US-007: Usuwanie użytkowników z zespołu
- Tytuł: Usuwanie członków zespołu
- Opis: Jako właściciel zespołu, chcę usuwać użytkowników z mojego zespołu, gdy ich obecność nie jest już potrzebna.
- Kryteria akceptacji:
  1. Właściciel zespołu może przeglądać listę członków zespołu
  2. Właściciel może wybrać użytkownika do usunięcia
  3. System wyświetla prośbę o potwierdzenie usunięcia
  4. Po potwierdzeniu, użytkownik jest usuwany z zespołu
  5. Usunięty użytkownik traci dostęp do dokumentów zespołu
  6. System potwierdza usunięcie użytkownika z zespołu

#### US-008: Zarządzanie dokumentami zespołu
- Tytuł: Zarządzanie dokumentami w ramach zespołu
- Opis: Jako członek zespołu, chcę zarządzać dokumentami w ramach zespołu, aby współpracować z innymi członkami.
- Kryteria akceptacji:
  1. Członek zespołu może przeglądać dokumenty dostępne dla zespołu
  2. Członek zespołu może dodawać nowe dokumenty do zespołu
  3. Członek zespołu może edytować dokumenty zespołu zgodnie z jego uprawnieniami
  4. Zmiany w dokumentach zespołu są widoczne dla wszystkich członków
  5. System informuje o tym, który członek zespołu ostatnio modyfikował dokument

### Zarządzanie dokumentami

#### US-009: Dodawanie nowego dokumentu
- Tytuł: Dodawanie dokumentu do systemu
- Opis: Jako zalogowany użytkownik, chcę dodać nowy dokument do systemu, aby móc go przechowywać i zarządzać nim.
- Kryteria akceptacji:
  1. Użytkownik może wybrać plik z dysku (obsługiwane formaty: markdown, doc, pdf, obrazy)
  2. System weryfikuje format i rozmiar pliku (limit 30 MB)
  3. Użytkownik może wprowadzić metadane dokumentu (tytuł, opis)
  4. System sugeruje kategorię na podstawie zawartości/nazwy dokumentu
  5. Użytkownik może wybrać lub zmienić sugerowaną kategorię
  6. Użytkownik może określić, czy dokument jest prywatny czy dostępny dla zespołu
  7. System potwierdza dodanie dokumentu
  8. System automatycznie generuje podsumowanie dokumentu przy pomocy AI

#### US-010: Przeglądanie listy dokumentów
- Tytuł: Przeglądanie dokumentów użytkownika
- Opis: Jako zalogowany użytkownik, chcę przeglądać listę moich dokumentów i dokumentów zespołów, aby znaleźć potrzebny mi dokument.
- Kryteria akceptacji:
  1. System wyświetla listę dokumentów należących do zalogowanego użytkownika
  2. System wyświetla listę dokumentów zespołów, do których należy użytkownik
  3. Lista zawiera podstawowe metadane dokumentów (tytuł, datę utworzenia, rozmiar, kategorię)
  4. Użytkownik może sortować listę według różnych kryteriów
  5. Użytkownik może filtrować listę według podstawowych kryteriów i przynależności do zespołu
  6. System wyświetla ostatnio używane dokumenty na stronie głównej

#### US-011: Edycja metadanych dokumentu
- Tytuł: Edycja informacji o dokumencie
- Opis: Jako zalogowany użytkownik, chcę edytować metadane dokumentu, aby aktualizować informacje o nim.
- Kryteria akceptacji:
  1. Użytkownik może zmienić tytuł dokumentu
  2. Użytkownik może zmienić opis dokumentu
  3. Użytkownik może zmienić kategorię dokumentu
  4. Użytkownik może zmienić dostępność dokumentu (prywatny/zespół)
  5. System blokuje dokument dla innych użytkowników podczas edycji
  6. System automatycznie zapisuje zmiany w metadanych
  7. System rejestruje zmianę jako nową wersję dokumentu

#### US-012: Usuwanie dokumentu
- Tytuł: Usuwanie dokumentu z systemu
- Opis: Jako zalogowany użytkownik, chcę usunąć dokument z systemu, gdy nie jest już potrzebny.
- Kryteria akceptacji:
  1. Użytkownik może wybrać dokument do usunięcia
  2. System weryfikuje uprawnienia użytkownika do usunięcia dokumentu
  3. System wyświetla prośbę o potwierdzenie usunięcia
  4. Po potwierdzeniu, dokument i wszystkie jego wersje są usuwane z systemu
  5. System potwierdza usunięcie dokumentu

#### US-013: Organizacja dokumentów w projekty/foldery
- Tytuł: Grupowanie dokumentów
- Opis: Jako zalogowany użytkownik, chcę organizować moje dokumenty w projekty lub foldery, aby łatwiej nimi zarządzać.
- Kryteria akceptacji:
  1. Użytkownik może tworzyć nowe projekty/foldery
  2. Użytkownik może przypisywać dokumenty do projektów/folderów
  3. Użytkownik może przenosić dokumenty między projektami/folderami
  4. Użytkownik może usuwać projekty/foldery (z opcją zachowania dokumentów)
  5. System wyświetla dokumenty pogrupowane według projektów/folderów
  6. Projekty/foldery mogą być przypisane do zespołu lub pozostać prywatne

### Wersjonowanie dokumentów

#### US-014: Aktualizacja zawartości dokumentu
- Tytuł: Aktualizacja dokumentu
- Opis: Jako zalogowany użytkownik, chcę zaktualizować zawartość dokumentu, zachowując historię zmian.
- Kryteria akceptacji:
  1. Użytkownik może wybrać dokument do aktualizacji
  2. Użytkownik może przesłać nową wersję pliku
  3. System weryfikuje format i rozmiar pliku
  4. System automatycznie tworzy nową wersję dokumentu
  5. System generuje różnice między wersjami przy pomocy AI
  6. System potwierdza aktualizację dokumentu

#### US-015: Przeglądanie historii wersji
- Tytuł: Historia wersji dokumentu
- Opis: Jako zalogowany użytkownik, chcę przeglądać historię wersji dokumentu, aby śledzić zmiany.
- Kryteria akceptacji:
  1. Użytkownik może wyświetlić listę wszystkich wersji dokumentu
  2. Lista zawiera informacje o każdej wersji (data utworzenia, autor, opis zmian)
  3. System wyświetla podsumowanie dokumentu dla pierwszej wersji
  4. System wyświetla różnice między wersjami generowane przez AI
  5. Użytkownik może przeglądać wersje chronologicznie

#### US-016: Przywracanie poprzedniej wersji
- Tytuł: Przywracanie wcześniejszej wersji dokumentu
- Opis: Jako zalogowany użytkownik, chcę przywrócić wcześniejszą wersję dokumentu, gdy aktualna wersja nie spełnia moich oczekiwań.
- Kryteria akceptacji:
  1. Użytkownik może wybrać wcześniejszą wersję do przywrócenia
  2. System wyświetla prośbę o potwierdzenie przywrócenia
  3. Po potwierdzeniu, wybrana wersja staje się aktualną wersją dokumentu
  4. System tworzy nową wersję zamiast nadpisywać historię
  5. System potwierdza przywrócenie wersji

### Wyszukiwanie i filtrowanie

#### US-017: Wyszukiwanie dokumentów
- Tytuł: Wyszukiwanie dokumentów po metadanych
- Opis: Jako zalogowany użytkownik, chcę wyszukiwać dokumenty według różnych kryteriów, aby szybko znaleźć potrzebne informacje.
- Kryteria akceptacji:
  1. Użytkownik może wyszukiwać dokumenty po tytule
  2. Użytkownik może wyszukiwać dokumenty po kategorii
  3. Użytkownik może wyszukiwać dokumenty po dacie utworzenia/modyfikacji
  4. Użytkownik może wyszukiwać dokumenty po autorze
  5. Użytkownik może wyszukiwać dokumenty według przynależności do zespołu
  6. System wyświetla wyniki wyszukiwania w postaci listy
  7. System informuje, gdy nie znaleziono dokumentów spełniających kryteria

#### US-018: Filtrowanie listy dokumentów
- Tytuł: Filtrowanie dokumentów
- Opis: Jako zalogowany użytkownik, chcę filtrować listę dokumentów, aby zawęzić wyniki do interesujących mnie pozycji.
- Kryteria akceptacji:
  1. Użytkownik może filtrować dokumenty według typu pliku
  2. Użytkownik może filtrować dokumenty według kategorii
  3. Użytkownik może filtrować dokumenty według daty
  4. Użytkownik może filtrować dokumenty według rozmiaru
  5. Użytkownik może filtrować dokumenty według przynależności (prywatne/zespół)
  6. System dynamicznie aktualizuje listę dokumentów zgodnie z zastosowanymi filtrami
  7. Użytkownik może łączyć różne filtry

### Administracja

#### US-019: Zarządzanie użytkownikami przez administratora
- Tytuł: Administracja użytkownikami
- Opis: Jako administrator, chcę zarządzać kontami użytkowników, aby utrzymać porządek w systemie.
- Kryteria akceptacji:
  1. Administrator może przeglądać listę wszystkich użytkowników
  2. Administrator może blokować/odblokowywać konta użytkowników
  3. Administrator może resetować hasła użytkowników
  4. Administrator może przypisywać role użytkownikom
  5. Administrator może usuwać konta użytkowników

#### US-020: Zarządzanie zespołami przez administratora
- Tytuł: Administracja zespołami
- Opis: Jako administrator, chcę mieć możliwość zarządzania zespołami, aby utrzymać porządek w systemie.
- Kryteria akceptacji:
  1. Administrator może przeglądać listę wszystkich zespołów
  2. Administrator może edytować dane zespołu
  3. Administrator może dezaktywować/aktywować zespoły
  4. Administrator może usuwać zespoły
  5. Administrator może przeglądać członków każdego zespołu

#### US-021: Konfiguracja kategorii
- Tytuł: Zarządzanie kategoriami dokumentów
- Opis: Jako administrator, chcę zarządzać systemem kategorii, aby zapewnić spójną organizację dokumentów.
- Kryteria akceptacji:
  1. Administrator może dodawać nowe kategorie główne
  2. Administrator może dodawać podkategorie do kategorii głównych
  3. Administrator może edytować nazwy kategorii
  4. Administrator może dezaktywować nieużywane kategorie
  5. Administrator może przeglądać statystyki użycia kategorii

## 6. Metryki sukcesu

### 6.1. Metryki funkcjonalne
1. Użytkownik może utworzyć konto i zalogować się do systemu
   - 100% nowych użytkowników może pomyślnie zakończyć proces rejestracji
   - Czas rejestracji nie przekracza 2 minut

2. Użytkownik może zarządzać dokumentami
   - Pomyślne dodanie dokumentu w 95% prób
   - Czas dodawania dokumentu (bez przesyłania) nie przekracza 1 minuty
   - Użytkownik może znaleźć dokument w systemie w czasie poniżej 30 sekund

3. System wersjonowania działa poprawnie
   - 100% zmian w dokumentach jest automatycznie wersjonowanych
   - Czas generowania podsumowania i różnic między wersjami nie przekracza 3 sekund
   - Przywracanie poprzedniej wersji kończy się sukcesem w 99% przypadków

4. System kategoryzacji i organizacji działa efektywnie
   - Dokładność automatycznych sugestii kategorii wynosi co najmniej 70%
   - Użytkownik może utworzyć i zorganizować strukturę folderów w czasie poniżej 2 minut

5. Zarządzanie zespołami funkcjonuje prawidłowo
   - 95% zaproszeń do zespołu zostaje pomyślnie wysłanych i dostarczonych
   - Członkowie zespołu mają natychmiastowy dostęp do dokumentów zespołu po dołączeniu
   - Dokumenty zespołu są odpowiednio zabezpieczone przed dostępem osób spoza zespołu

### 6.2. Metryki techniczne
1. Wydajność systemu
   - Czas odpowiedzi dla podstawowych operacji poniżej 1 sekundy
   - Czas ładowania strony głównej poniżej 2 sekund
   - Obsługa równoczesnej pracy co najmniej 50 użytkowników

2. Stabilność i niezawodność
   - Dostępność systemu na poziomie 99.9%
   - Wszystkie testy jednostkowe przechodzą pomyślnie
   - Zero utraconych danych użytkowników

3. Bezpieczeństwo
   - Zero przypadków nieuprawnionego dostępu do danych
   - 100% zgodność z wymogami bezpieczeństwa ASP.NET Core Identity
   - Poprawna izolacja danych między różnymi zespołami

### 6.3. Metryki biznesowe
1. Zgodność z wymaganiami kursu
   - Spełnienie wszystkich kryteriów zaliczeniowych projektu
   - Terminowe ukończenie wszystkich faz rozwoju

2. Jakość kodu i dokumentacji
   - Pokrycie kodu testami jednostkowymi na poziomie co najmniej 80%
   - Pełna dokumentacja kodu zgodna ze standardami
   - Czytelny i utrzymywalny kod zgodny z konwencjami C#

3. Przygotowanie do rozbudowy
   - Architektura systemu pozwala na łatwe dodawanie nowych funkcjonalności
   - Struktura bazy danych umożliwia przyszłe rozszerzenie dla małych firm 