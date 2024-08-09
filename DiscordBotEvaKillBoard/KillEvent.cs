namespace DiscordBotEvaKillBoard
{
    public class KillEvent
    {
        public string Action { get; set; } = String.Empty;
        public long KillID { get; set; }
        public long CharacterId { get; set; }
        public long CorporationId { get; set; }
        public long AllianceId { get; set; }
        public int ShipTypeId { get; set; }
        public int GroupId { get; set; }
        public string Url { get; set; } = String.Empty;
        public string Hash { get; set; } = String.Empty;
        public string Channel { get; set; } = String.Empty;
    }

}
