#nullable enable

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;

namespace Ur.Data {

    class GameObject : INotifyPropertyChanged {
        public string Id { get; } = Guid.NewGuid().ToString();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    record TilePosition(int Column, int Row) {
        public int Progress {
            get => (Column, Row) switch {
                (0, _) => -1,
                (4, _) => -1,
                (1, 0) => 12,
                (1, 1) => 13,
                (1, 4) => 0,
                (1, 5) => 1,
                (1, 6) => 2,
                (1, 7) => 3,
                (3, 4) => 0,
                (3, 5) => 1,
                (3, 6) => 2,
                (3, 7) => 3,
                (2, _) => 4+7-Row,
                (3, 0) => 12,
                (3, 1) => 13,
                _ => 14
            };            
        }
        public static TilePosition FromProgress(int progress, Player player) {
            if (progress < 0) {
                if (player.Index == 0) {
                    return new TilePosition(0, 0);
                } else {
                    return new TilePosition(4, 0);
                }
            }
            else if (progress <= 3) {
                if (player.Index == 0) {
                    return new TilePosition(1, 4 + progress);
                } else {
                    return new TilePosition(3, 4 + progress);
                }
            }
            else if (progress <= 11) {
                return new TilePosition(2, 7 - (progress - 4));
            }
            return new TilePosition(0, 0);
        }
    }

    class Game : GameObject
    {
        public Player[] Players { get; } = new Player[2] {
            new Player(0),
            new Player(1) { PieceColor = "black" }
        };
        readonly GamePiece[] pieces;
        public GamePiece[] Pieces => pieces;
        public int ActivePlayerIndex { get; private set; }
        public Player ActivePlayer => Players[ActivePlayerIndex];
        public int RolledPlacesToMove
        {
            get => rolledPlacesToMove;
            private set
            {
                rolledPlacesToMove = value;
                OnPropertyChanged(nameof(RolledPlacesToMove));
            }
        }
        readonly Random random = new Random();
        public Game()
        {
            pieces = new GamePiece[7 * Players.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i] = new GamePiece(Players[i / 7], i % 7);
            }
            rolledPlacesToMove = random.Next(5);
        }

        string status = "";
        private int rolledPlacesToMove = 1;

        public string Status
        {
            get => status;
            set
            {
                if (status != value)
                {
                    status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public async Task MovePieceAsync(GamePiece piece)
        {
            if (piece.Player != Players[ActivePlayerIndex])
            {
                Status = "Not your turn!";
                return;
            }

            var newPosition = TilePosition.FromProgress(piece.Position.Progress + RolledPlacesToMove, piece.Player);
            if (newPosition == piece.Position || !CanMoveTo(newPosition, piece.Player))
            {
                Status = "Can't move there!";
                await Task.Delay(3000);
                Status = "";
                return;
            }
            else
            {
                var capturedPiece = FindPieceAt(newPosition);
                if (capturedPiece != null && capturedPiece.Player != piece.Player) {
                    Status = $"Captured! {capturedPiece.Color}";
                    capturedPiece.Position = new TilePosition(capturedPiece.Player.OutsideColumn,
                                                              1 + capturedPiece.PieceIndex);
                }
                piece.Position = newPosition;
            }

            await NextTurnAsync();
        }

        public bool CanMoveTo(TilePosition position, Player player)
        {
            var otherPiece = FindPieceAt(position);
            return otherPiece switch {
                null => true,
                GamePiece p => p.Player != player
            };
        }

        GamePiece? FindPieceAt(TilePosition position)
        {
            return pieces.FirstOrDefault(p => p.Position == position);
        }

        async Task NextTurnAsync()
        {
            ActivePlayerIndex = (ActivePlayerIndex + 1) % Players.Length;
            RolledPlacesToMove = random.Next(5);

            while (RolledPlacesToMove == 0) {
                Status = "Rolled 0, no move!";
                await Task.Delay(3000);
                Status = "";
                ActivePlayerIndex = (ActivePlayerIndex + 1) % Players.Length;
                RolledPlacesToMove = random.Next(5);
            }
        }
    }

    class Player : GameObject {
        public string PieceColor { get; set; } = "white";
        public int Index { get; }
        public int OutsideColumn => Index == 0 ? 0 : 4;
        public Player(int index)
        {
            Index = index;
        }
    }

    class GamePiece : GameObject {
        public Player Player { get; private set; }
        public int PieceIndex { get; }

        public string Color => Player.PieceColor;
        public TilePosition Position { get; set; }
        public GamePiece(Player player, int pieceIndex)
        {
            Player = player;
            PieceIndex = pieceIndex;
            Position = new TilePosition (
                Color == "white" ? 0 : 4,
                pieceIndex + 1);
        }
    }

    
}