using System;
using System.Collections.Generic;

namespace UnoGame
{
    public enum CardColor { Red, Yellow, Green, Blue }
    public enum CardType { Number, Skip, Reverse, DrawTwo }

    public class Card
    {
        public CardColor Color { get; set; }
        public CardType Type { get; set; }
        public int Number { get; set; } // 0-9 om det är ett sifferkort

        public Card(CardColor color, CardType type, int number = -1)
        {
            Color = color;
            Type = type;
            Number = number;
        }

        public bool CanPlayOn(Card topCard)
        {
            return Color == topCard.Color
                || (Type == CardType.Number && topCard.Type == CardType.Number && Number == topCard.Number)
                || (Type != CardType.Number && Type == topCard.Type);
        }

        public override string ToString()
        {
            return Type == CardType.Number ? $"{Color} {Number}" : $"{Color} {Type}";
        }
    }

    public class Player
    {
        public string Name { get; set; }
        public List<Card> Hand { get; set; } = new List<Card>();

        public Player(string name)
        {
            Name = name;
        }
    }

    class Program
    {
        static Random rand = new Random();

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== UNO SPELET ===");

            // 1 & 2: Starta spel och välj 2-4 spelare
            int playerCount = 0;
            while (playerCount < 2 || playerCount > 4)
            {
                Console.Write("Ange antal spelare (2-4): ");
                int.TryParse(Console.ReadLine(), out playerCount);
            }

            List<Player> players = new List<Player>();
            for (int i = 1; i <= playerCount; i++)
            {
                players.Add(new Player($"Spelare {i}"));
            }

            // 3: Skapa och blanda kortlek
            List<Card> deck = CreateDeck();
            Shuffle(deck);

            // 4: Dela ut 7 startkort per spelare
            foreach (var player in players)
            {
                for (int i = 0; i < 7; i++)
                {
                    player.Hand.Add(DrawCard(deck));
                }
            }

            List<Card> discardPile = new List<Card>();
            Card topCard = DrawCard(deck);
            discardPile.Add(topCard);

            int currentPlayerIndex = 0;
            int direction = 1; // 1 = medsols, -1 = motsols
            bool gameRunning = true;

            while (gameRunning)
            {
                Player current = players[currentPlayerIndex];
                Console.WriteLine("\n----------------------------------");
                Console.Write("Tur: ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(current.Name);
                Console.ResetColor();

                Console.Write("Översta kortet på högen: ");
                PrintCard(topCard);
                Console.WriteLine();

                // 9: Utseende - Visa hand med kortens respektive färger
                Console.WriteLine("Dina kort:");
                for (int i = 0; i < current.Hand.Count; i++)
                {
                    Console.Write($"[{i + 1}] ");
                    PrintCard(current.Hand[i]);
                    Console.Write("  ");
                }
                Console.WriteLine();

                // 5: Lägga giltigt kort eller dra nytt
                Card playedCard = null;
                bool hasValidCard = current.Hand.Exists(c => c.CanPlayOn(topCard));

                if (!hasValidCard)
                {
                    Console.WriteLine("Du har inget giltigt kort. Du drar ett kort...");
                    Card drawn = DrawCard(deck);
                    current.Hand.Add(drawn);
                    Console.Write("Du drog: ");
                    PrintCard(drawn);
                    Console.WriteLine();

                    if (drawn.CanPlayOn(topCard))
                    {
                        Console.Write("Vill du lägga det dragna kortet? (j/n): ");
                        if (Console.ReadLine()?.Trim().ToLower() == "j")
                        {
                            playedCard = drawn;
                            current.Hand.Remove(drawn);
                        }
                    }
                }
                else
                {
                    int choice = -1;
                    while (choice < 0 || choice > current.Hand.Count)
                    {
                        Console.Write("Välj kortnummer att spela (eller 0 för att dra ett kort): ");
                        if (int.TryParse(Console.ReadLine(), out choice))
                        {
                            if (choice == 0)
                            {
                                Card drawn = DrawCard(deck);
                                current.Hand.Add(drawn);
                                Console.Write("Du drog: ");
                                PrintCard(drawn);
                                Console.WriteLine();
                                break;
                            }
                            else if (choice >= 1 && choice <= current.Hand.Count)
                            {
                                if (current.Hand[choice - 1].CanPlayOn(topCard))
                                {
                                    playedCard = current.Hand[choice - 1];
                                    current.Hand.RemoveAt(choice - 1);
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("Kortet matchar inte färg eller valör!");
                                    choice = -1;
                                }
                            }
                        }
                    }
                }

                if (playedCard != null)
                {
                    topCard = playedCard;
                    discardPile.Add(topCard);

                    // 6: Regel - Meddela när spelaren har 1 kort kvar (UNO!)
                    if (current.Hand.Count == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"\n*** UNO! {current.Name} har bara 1 kort kvar! ***\n");
                        Console.ResetColor();
                    }

                    // 8: Logik - Utse vinnare och avsluta
                    if (current.Hand.Count == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("\n==================================");
                        Console.WriteLine($"GRATTIS! {current.Name} vinner spelet!");
                        Console.WriteLine("==================================");
                        Console.ResetColor();
                        gameRunning = false;
                        break;
                    }

                    // 7: Regel - Utlös specialkortseffekter
                    if (playedCard.Type == CardType.Skip)
                    {
                        Console.WriteLine("Hoppa över! Nästa spelare förlorar sin tur.");
                        currentPlayerIndex = GetNextIndex(currentPlayerIndex, direction, players.Count);
                    }
                    else if (playedCard.Type == CardType.Reverse)
                    {
                        Console.WriteLine("Vänd! Spelriktningen ändras.");
                        direction *= -1;
                    }
                    else if (playedCard.Type == CardType.DrawTwo)
                    {
                        int nextPlayer = GetNextIndex(currentPlayerIndex, direction, players.Count);
                        Console.WriteLine($"{players[nextPlayer].Name} måste dra 2 kort och hoppa över sin tur!");
                        players[nextPlayer].Hand.Add(DrawCard(deck));
                        players[nextPlayer].Hand.Add(DrawCard(deck));
                        currentPlayerIndex = nextPlayer;
                    }
                }

                currentPlayerIndex = GetNextIndex(currentPlayerIndex, direction, players.Count);
            }
        }

        static int GetNextIndex(int current, int direction, int total)
        {
            int next = (current + direction) % total;
            return next < 0 ? next + total : next;
        }

        static List<Card> CreateDeck()
        {
            List<Card> deck = new List<Card>();
            foreach (CardColor color in Enum.GetValues(typeof(CardColor)))
            {
                for (int i = 0; i <= 9; i++)
                {
                    deck.Add(new Card(color, CardType.Number, i));
                    if (i != 0) deck.Add(new Card(color, CardType.Number, i));
                }

                for (int i = 0; i < 2; i++)
                {
                    deck.Add(new Card(color, CardType.Skip));
                    deck.Add(new Card(color, CardType.Reverse));
                    deck.Add(new Card(color, CardType.DrawTwo));
                }
            }
            return deck;
        }

        static void Shuffle(List<Card> deck)
        {
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int k = rand.Next(i + 1);
                var temp = deck[i];
                deck[i] = deck[k];
                deck[k] = temp;
            }
        }

        static Card DrawCard(List<Card> deck)
        {
            if (deck.Count == 0)
            {
                deck.AddRange(CreateDeck());
                Shuffle(deck);
            }
            Card card = deck[0];
            deck.RemoveAt(0);
            return card;
        }

        // 9: Skriv ut kort med konsolfärg baserat på kortfärg
        static void PrintCard(Card card)
        {
            Console.ForegroundColor = card.Color switch
            {
                CardColor.Red => ConsoleColor.Red,
                CardColor.Yellow => ConsoleColor.Yellow,
                CardColor.Green => ConsoleColor.Green,
                CardColor.Blue => ConsoleColor.Blue,
                _ => ConsoleColor.White
            };
            Console.Write($"[{card}]");
            Console.ResetColor();
        }
    }
}
