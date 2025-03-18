using System;
using System.Net;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace TicTacToeFTP
{
    // Repräsentiert den Zustand eines Spiels
    public class GameState
    {
        public string LobbyName { get; set; }
        public string Player1 { get; set; }
        public string Player2 { get; set; }
        public string CurrentTurn { get; set; }
        public string[] Board { get; set; }
        public string Status { get; set; } // "waiting", "ongoing", "finished"
        public string Winner { get; set; }
    }

    class Program
    {
        // Lädt den Spielzustand von der FTP-Datei herunter
        static string DownloadGameState(string ftpServerUrl, string lobbyName, string ftpUser, string ftpPass)
        {
            string fileUrl = ftpServerUrl + "/" + lobbyName + ".json";
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(fileUrl);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(ftpUser, ftpPass);
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Download: " + ex.Message);
                return null;
            }
        }

        // Lädt den (aktualisierten) Spielzustand auf den FTP-Server hoch
        static bool UploadGameState(string ftpServerUrl, string lobbyName, string ftpUser, string ftpPass, string content)
        {
            string fileUrl = ftpServerUrl + "/" + lobbyName + ".json";
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(fileUrl);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(ftpUser, ftpPass);
                byte[] fileContents = Encoding.UTF8.GetBytes(content);
                request.ContentLength = fileContents.Length;
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    // Optional: Statusbeschreibung auswerten
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Upload: " + ex.Message);
                return false;
            }
        }

        // Löscht die Spieldatei vom FTP-Server
        static bool DeleteGameState(string ftpServerUrl, string lobbyName, string ftpUser, string ftpPass)
        {
            string fileUrl = ftpServerUrl + "/" + lobbyName + ".json";
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(fileUrl);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(ftpUser, ftpPass);
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    // Optional: Überprüfe response.StatusDescription
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Löschen der Datei: " + ex.Message);
                return false;
            }
        }

        // Zeigt das Spielfeld in der Konsole an
        static void DisplayBoard(string[] board)
        {
            Console.Clear();
            Console.WriteLine("Tic Tac Toe");
            Console.WriteLine();
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine(" {0} | {1} | {2} ", board[i * 3], board[i * 3 + 1], board[i * 3 + 2]);
                if (i < 2)
                    Console.WriteLine("---+---+---");
            }
            Console.WriteLine();
        }

        // Prüft, ob der gegebene Symbol (X oder O) eine Gewinnkombination hat
        static bool CheckWin(string[] board, string symbol)
        {
            int[][] winConditions = new int[][]
            {
                new int[]{0,1,2}, new int[]{3,4,5}, new int[]{6,7,8}, // Reihen
                new int[]{0,3,6}, new int[]{1,4,7}, new int[]{2,5,8}, // Spalten
                new int[]{0,4,8}, new int[]{2,4,6}                    // Diagonalen
            };

            foreach (var condition in winConditions)
            {
                if (board[condition[0]] == symbol &&
                    board[condition[1]] == symbol &&
                    board[condition[2]] == symbol)
                    return true;
            }
            return false;
        }

        // Prüft, ob alle Felder belegt sind (Unentschieden)
        static bool CheckDraw(string[] board)
        {
            foreach (var cell in board)
            {
                if (string.IsNullOrWhiteSpace(cell))
                    return false;
            }
            return true;
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Tic Tac Toe Multiplayer über FTP Server");
            string ftpServerUrl = "ftp://ftpupload.net";
            string ftpUser = "if0_38546715";
            string ftpPass = "AKEkHsjAxx2Y9";

            Console.WriteLine("\nOptionen:");
            Console.WriteLine("1. Lobby erstellen");
            Console.WriteLine("2. Lobby beitreten");
            Console.Write("Wähle Option (1 oder 2): ");
            string option = Console.ReadLine();

            Console.Write("Dein Spielername: ");
            string myName = Console.ReadLine();
            GameState gameState = null;
            string lobbyName = "";

            if (option == "1")
            {
                Console.Write("Lobby Name: ");
                // Einheitliche Pfadangabe: "htdocs/" voranstellen
                lobbyName = "htdocs/" + Console.ReadLine().Trim();

                // Erstelle einen neuen Spielzustand
                gameState = new GameState
                {
                    LobbyName = lobbyName,
                    Player1 = myName,
                    Player2 = "",
                    CurrentTurn = myName, // Spielstart beim Ersteller
                    Board = new string[9],
                    Status = "waiting",
                    Winner = ""
                };
                // Felder initialisieren
                for (int i = 0; i < 9; i++)
                    gameState.Board[i] = " ";

                // Upload des initialen Zustands
                string json = JsonSerializer.Serialize(gameState);
                if (!UploadGameState(ftpServerUrl, lobbyName, ftpUser, ftpPass, json))
                {
                    Console.WriteLine("Lobby konnte nicht erstellt werden.");
                    return;
                }
                Console.WriteLine("Lobby erstellt. Warte auf zweiten Spieler...");

                // Warten, bis ein zweiter Spieler beitritt
                while (true)
                {
                    Thread.Sleep(1000);
                    string content = DownloadGameState(ftpServerUrl, lobbyName, ftpUser, ftpPass);
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        Console.WriteLine("Warte auf Datei...");
                        continue;
                    }
                    try
                    {
                        gameState = JsonSerializer.Deserialize<GameState>(content);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Fehler beim Deserialisieren: " + ex.Message);
                        continue;
                    }
                    if (!string.IsNullOrEmpty(gameState.Player2))
                    {
                        gameState.Status = "ongoing";
                        json = JsonSerializer.Serialize(gameState);
                        UploadGameState(ftpServerUrl, lobbyName, ftpUser, ftpPass, json);
                        Console.WriteLine("Zweiter Spieler beigetreten. Spiel startet!");
                        break;
                    }
                    Console.WriteLine("Warte auf zweiten Spieler...");
                }
            }
            else if (option == "2")
            {
                Console.Write("Lobby Name: ");
                // Einheitlicher Pfad: "htdocs/" voranstellen
                lobbyName = "htdocs/" + Console.ReadLine().Trim();

                // Mehrfache Versuche, den Spielzustand zu laden
                string content = null;
                int retryCount = 0;
                while (retryCount < 5 && string.IsNullOrWhiteSpace(content))
                {
                    content = DownloadGameState(ftpServerUrl, lobbyName, ftpUser, ftpPass);
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        Console.WriteLine("Datei nicht gefunden, versuche erneut in 3 Sekunden...");
                        Thread.Sleep(3000);
                        retryCount++;
                    }
                }
                if (string.IsNullOrWhiteSpace(content))
                {
                    Console.WriteLine("Lobby nicht gefunden oder Datei ist leer.");
                    return;
                }

                try
                {
                    gameState = JsonSerializer.Deserialize<GameState>(content);
                    if (gameState == null)
                    {
                        Console.WriteLine("Fehler beim Parsen des Spielzustands.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler beim Deserialisieren des Spielzustands: " + ex.Message);
                    return;
                }

                if (!string.IsNullOrEmpty(gameState.Player2))
                {
                    Console.WriteLine("Lobby ist bereits voll.");
                    return;
                }

                gameState.Player2 = myName;
                gameState.Status = "ongoing";
                string json = JsonSerializer.Serialize(gameState);
                if (!UploadGameState(ftpServerUrl, lobbyName, ftpUser, ftpPass, json))
                {
                    Console.WriteLine("Fehler beim Beitreten der Lobby.");
                    return;
                }
                Console.WriteLine("Erfolgreich der Lobby beigetreten. Spiel startet!");
            }
            else
            {
                Console.WriteLine("Ungültige Option.");
                return;
            }

            // Bestimme das Symbol des Spielers
            string mySymbol = (gameState.Player1 == myName) ? "X" : "O";
            // Gegnername ermitteln (falls vorhanden)
            string opponent = (gameState.Player1 == myName) ? gameState.Player2 : gameState.Player1;

            // Spielschleife
            while (gameState.Status == "ongoing")
            {
                string content = DownloadGameState(ftpServerUrl, lobbyName, ftpUser, ftpPass);
                if (string.IsNullOrWhiteSpace(content))
                {
                    Console.WriteLine("Fehler beim Laden des Spiels. Versuche erneut...");
                    Thread.Sleep(3000);
                    continue;
                }
                try
                {
                    gameState = JsonSerializer.Deserialize<GameState>(content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler beim Deserialisieren: " + ex.Message);
                    Thread.Sleep(3000);
                    continue;
                }

                if (gameState.Board == null || gameState.Board.Length != 9)
                {
                    Console.WriteLine("Ungültiges Spielfeld. Spiel wird beendet.");
                    return;
                }

                DisplayBoard(gameState.Board);
                Console.WriteLine("Spieler 1 (X): " + gameState.Player1);
                Console.WriteLine("Spieler 2 (O): " + gameState.Player2);

                if (CheckWin(gameState.Board, "X"))
                {
                    gameState.Status = "finished";
                    gameState.Winner = gameState.Player1;
                }
                else if (CheckWin(gameState.Board, "O"))
                {
                    gameState.Status = "finished";
                    gameState.Winner = gameState.Player2;
                }
                else if (CheckDraw(gameState.Board))
                {
                    gameState.Status = "finished";
                    gameState.Winner = "Unentschieden";
                }

                if (gameState.Status == "finished")
                {
                    DisplayBoard(gameState.Board);
                    if (gameState.Winner == "Unentschieden")
                        Console.WriteLine("Das Spiel endet unentschieden!");
                    else
                        Console.WriteLine("Gewinner: " + gameState.Winner);
                    string finalJson = JsonSerializer.Serialize(gameState);
                    UploadGameState(ftpServerUrl, lobbyName, ftpUser, ftpPass, finalJson);

                    // Lösche die Spieldatei nach Spielende
                    if (DeleteGameState(ftpServerUrl, lobbyName, ftpUser, ftpPass))
                    {
                        Console.WriteLine("Die Spieldatei wurde erfolgreich gelöscht.");
                    }
                    else
                    {
                        Console.WriteLine("Fehler beim Löschen der Spieldatei.");
                    }
                    break;
                }

                if (gameState.CurrentTurn == myName)
                {
                    Console.Write("Dein Zug (Feld 1-9): ");
                    string input = Console.ReadLine();
                    if (int.TryParse(input, out int move))
                    {
                        move -= 1; // Umrechnung in 0-indexiertes Array
                        if (move >= 0 && move < 9)
                        {
                            if (gameState.Board[move] == " ")
                            {
                                gameState.Board[move] = mySymbol;
                                gameState.CurrentTurn = (gameState.CurrentTurn == gameState.Player1) ? gameState.Player2 : gameState.Player1;
                                string updatedJson = JsonSerializer.Serialize(gameState);
                                if (!UploadGameState(ftpServerUrl, lobbyName, ftpUser, ftpPass, updatedJson))
                                {
                                    Console.WriteLine("Fehler beim Aktualisieren des Spiels. Versuche es erneut...");
                                    continue;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Ungültiger Zug, Feld bereits besetzt. Drücke Enter und versuche es erneut.");
                                Console.ReadLine();
                            }
                        }
                        else
                        {
                            Console.WriteLine("Ungültige Eingabe. Drücke Enter und versuche es erneut.");
                            Console.ReadLine();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Ungültige Eingabe. Drücke Enter und versuche es erneut.");
                        Console.ReadLine();
                    }
                }
                else
                {
                    Console.WriteLine("Warte auf den Zug des Gegenspielers...");
                    Thread.Sleep(3000);
                }
            }
            Console.WriteLine("Spiel beendet. Drücke eine Taste zum Beenden.");
            Console.ReadKey();
        }
    }
}
