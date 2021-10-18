#nullable enable


namespace Ur.Data
{
    public interface ITilePosition
    {
        int Column { get; }
        int Row { get; }
    }

    public static class TilePositionEx {

        public static int GetProgress(this ITilePosition t) => (t.Column, t.Row) switch {
                (0, _) => -1,
                (4, _) => -1,
                (1, 0) => 12,
                (1, 1) => 13,
                (1, 2) => 14,
                (1, 4) => 0,
                (1, 5) => 1,
                (1, 6) => 2,
                (1, 7) => 3,
                (3, 4) => 0,
                (3, 5) => 1,
                (3, 6) => 2,
                (3, 7) => 3,
                (2, _) => 4+7-t.Row,
                (3, 0) => 12,
                (3, 1) => 13,
                (3, 2) => 14,
                _ => 15
            };            
        
    }
    public record TilePosition(int Column, int Row) : ITilePosition {
        
        public static TilePosition? FromProgress(int progress, Player player) {
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
            else if (progress <= 14) {
                if (player.Index == 0) {
                    return new TilePosition(1, progress - 12);
                } else {
                    return new TilePosition(3, progress - 12);
                }
            }
            return null;
        }
    }

    
}