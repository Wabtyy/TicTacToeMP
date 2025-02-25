using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

class TicTacToe
{
    static string lobbyFile;
    static char[] board;
    static bool gameOver = false;
    static string lastBoardState = "";
    static char localPlayer;
    static async Task Main()
    {
        Console.Write("Gib den Namen der Lobby ein: ");
        string lobbyName = Console.ReadLine();
        lobbyFile = $"{lobbyName}.txt";
        Console.Clear();

        // Wenn die Lobby noch nicht existiert, erstellen wir sie
        if (!File.Exists(lobbyFile))
        {
            Console.WriteLine("Erstelle neue Lobby. Du bist der erste Spieler (X).");
            board = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
          localPlayer = 'X';
            SaveGame('X');  // 'X' startet immer das Spiel
        }
        else
        {
            localPlayer = 'O';
            Console.WriteLine("Lobby existiert bereits. Du bist der zweite Spieler (O).");
        }
        Console.Read();
        Task.Run(() => AutoRefresh());  // Alle 1 Sekunde das Spielfeld automatisch aktualisieren

        AppDomain.CurrentDomain.ProcessExit += (sender, e) => { if (File.Exists(lobbyFile)) File.Delete(lobbyFile); };

        while (!gameOver)
        {
            char currentPlayer = GetCurrentPlayer();


            // Nur der aktuelle Spieler darf einen Zug machen
            if (currentPlayer == localPlayer)
            {
                Console.WriteLine($"Spieler {currentPlayer}, wähle ein Feld (1-9): ");
                if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= 9 && board[choice - 1] == choice.ToString()[0])
                {
                    // Zug ausführen
                    board[choice - 1] = currentPlayer;
                    if (CheckWin())
                    {
                        SaveGame(currentPlayer);
                        Console.Clear();
                        DrawBoard();
                        Console.WriteLine($"Spieler {currentPlayer} gewinnt!");
                        File.Delete(lobbyFile);  // Lobby-Datei löschen, um das Spiel zu beenden
                        gameOver = true;
                        break;
                    }
                    TogglePlayer();  // Nach dem Zug den Spieler wechseln
                    SaveGame(GetCurrentPlayer());  // Speichern des aktuellen Spielstands
                }
                else
                {
                }
            }
            else
            {
                // Wenn der Spieler nicht dran ist, wartet er
                Console.Clear();
                DrawBoard();
                Console.WriteLine("Warte auf den anderen Spieler...");

            }

            // Verzögere die Schleife, um zu verhindern, dass das Programm zu schnell läuft
            await Task.Delay(1000);
        }
    }

    static void AutoRefresh()
    {
        while (!gameOver)
        {
            // Lesen und Prüfen der Datei auf Änderungen
            string currentBoardState = File.Exists(lobbyFile) ? File.ReadAllText(lobbyFile) : "";
            if (currentBoardState != lastBoardState)
            {
                lastBoardState = currentBoardState;
                LoadGame();
                Console.Clear();
                DrawBoard();
            }
            Thread.Sleep(1000);
        }
    }

    static void DrawBoard()
    {
        Console.WriteLine(" {0} | {1} | {2} ", board[0], board[1], board[2]);
        Console.WriteLine("---+---+---");
        Console.WriteLine(" {0} | {1} | {2} ", board[3], board[4], board[5]);
        Console.WriteLine("---+---+---");
        Console.WriteLine(" {0} | {1} | {2} ", board[6], board[7], board[8]);
    }

    static bool CheckWin()
    {
        int[,] winConditions =
        {
            { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 }, // Horizontal
            { 0, 3, 6 }, { 1, 4, 7 }, { 2, 5, 8 }, // Vertikal
            { 0, 4, 8 }, { 2, 4, 6 }              // Diagonal
        };

        for (int i = 0; i < winConditions.GetLength(0); i++)
        {
            if (board[winConditions[i, 0]] == board[winConditions[i, 1]] &&
                board[winConditions[i, 1]] == board[winConditions[i, 2]])
            {
                return true;
            }
        }
        return false;
    }

    static void SaveGame(char currentPlayer)
    {
        // Sperren des Zuges: Nur einer darf auf einmal schreiben
        lock (lobbyFile)
        {
            File.WriteAllText(lobbyFile, currentPlayer + "\n" + new string(board));
        }
    }

    static void LoadGame()
    {
        if (File.Exists(lobbyFile))
        {
            string[] lines = File.ReadAllLines(lobbyFile);
            board = lines[1].ToCharArray();
        }
    }

    static char GetCurrentPlayer()
    {
        // Der aktuelle Spieler ist derjenige, der in der Lobby-Datei gespeichert ist
        return File.Exists(lobbyFile) ? File.ReadAllLines(lobbyFile)[0][0] : 'X';
    }

    static char GetLocalPlayer()
    {
        // Der lokale Spieler ist derjenige, der das Fenster geöffnet hat
        return File.Exists(lobbyFile) ? 'X' : 'O';
    }

    static void TogglePlayer()
    {
        char currentPlayer = GetCurrentPlayer();
        char nextPlayer = (currentPlayer == 'X') ? 'O' : 'X';

        // Sperren des Zuges: Nur einer darf auf einmal schreiben
        lock (lobbyFile)
        {
            SaveGame(nextPlayer);  // Wechselt den Spieler und speichert den neuen Spieler in der Datei
        }
    }

    static void DisplayPlayerInfo()
    {
        char localPlayer = GetLocalPlayer();
        Console.WriteLine($"Du bist Spieler {localPlayer}.");
    }
}
