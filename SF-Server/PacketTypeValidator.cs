using System;

namespace SFServer
{
    /// <summary>
    /// Simple validation to ensure packet type constants match between client and server
    /// </summary>
    public static class PacketTypeValidator
    {
        public static void ValidatePacketTypes()
        {
            Console.WriteLine("=== Packet Type Validation ===");
            
            // Validate that our hardcoded packet type values match the actual enum
            var clientJoinedValue = (byte)SfPacketType.ClientJoined;
            var clientSpawnedValue = (byte)SfPacketType.ClientSpawned;
            var mapChangeValue = (byte)SfPacketType.MapChange;
            var clientReadyUpValue = (byte)SfPacketType.ClientReadyUp;
            
            Console.WriteLine($"ClientJoined packet type: {clientJoinedValue} (expected: 2)");
            Console.WriteLine($"ClientSpawned packet type: {clientSpawnedValue} (expected: 8)");
            Console.WriteLine($"MapChange packet type: {mapChangeValue} (expected: 18)");
            Console.WriteLine($"ClientReadyUp packet type: {clientReadyUpValue} (expected: 9)");
            
            // Validate critical packet types
            if (clientJoinedValue != 2)
            {
                throw new InvalidOperationException($"ClientJoined packet type mismatch! Got {clientJoinedValue}, expected 2");
            }
            
            if (clientSpawnedValue != 8)
            {
                throw new InvalidOperationException($"ClientSpawned packet type mismatch! Got {clientSpawnedValue}, expected 8");
            }
            
            if (mapChangeValue != 18)
            {
                throw new InvalidOperationException($"MapChange packet type mismatch! Got {mapChangeValue}, expected 18");
            }
            
            if (clientReadyUpValue != 9)
            {
                throw new InvalidOperationException($"ClientReadyUp packet type mismatch! Got {clientReadyUpValue}, expected 9");
            }
            
            Console.WriteLine("✅ All packet type validations passed!");
            Console.WriteLine("Client-side hardcoded values should match these enum values.");
        }
    }
}