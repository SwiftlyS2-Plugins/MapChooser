namespace MapChooser.Models;

public class RtvConfig
{
    public bool Enabled { get; set; } = true;
    public bool EnabledInWarmup { get; set; } = false;
    public bool NominationEnabled { get; set; } = true;
    public int MinPlayers { get; set; } = 0;
    public int MinRounds { get; set; } = 0;
    public bool ChangeMapImmediately { get; set; } = true;
    public int ChangeMapDelay { get; set; } = 3;
    public int MapsToShow { get; set; } = 6;
    public int VoteDuration { get; set; } = 30;
    public int VotePercentage { get; set; } = 60;
    public int VoteCooldownTime { get; set; } = 300;
}

public class VotemapConfig
{
    public bool Enabled { get; set; } = true;
    public int VotePercentage { get; set; } = 60;
    public bool ChangeMapImmediately { get; set; } = false;
    public int MinPlayers { get; set; } = 0;
}

public class EndOfMapConfig
{
    public bool Enabled { get; set; } = true;
    public int MapsToShow { get; set; } = 6;
    public int VoteDuration { get; set; } = 30;
    public int ChangeMapDelay { get; set; } = 5;
    public int TriggerSecondsBeforeEnd { get; set; } = 120;
    public int TriggerRoundsBeforeEnd { get; set; } = 4;
    public bool AllowExtend { get; set; } = true;
    public int ExtendTimeStep { get; set; } = 15;
    public int ExtendRoundStep { get; set; } = 5;
    public int ExtendLimit { get; set; } = 3;
}

public class ExtendMapConfig
{
    public bool Enabled { get; set; } = true;
    public bool EnabledInWarmup { get; set; } = false;
    public int MinPlayers { get; set; } = 0;
    public int MinRounds { get; set; } = 0;
    public int VotePercentage { get; set; } = 60;
}

public class CycleConfig
{
    public bool Enabled { get; set; } = false;
    public bool RandomOrder { get; set; } = false;
    public string AddMapPermission { get; set; } = "admin.changemap";
    public string RemoveMapPermission { get; set; } = "admin.changemap";
    public string CycleMenuPermission { get; set; } = "admin.changemap";
}

public class CommandsConfig
{
    public string Rtv { get; set; } = "rtv";
    public string UnRtv { get; set; } = "unrtv";
    public string Nominate { get; set; } = "nominate,nom,yd";
    public string Timeleft { get; set; } = "timeleft";
    public string Nextmap { get; set; } = "nextmap";
    public string Votemap { get; set; } = "votemap";
    public string Revote { get; set; } = "revote";
    public string SetNextMap { get; set; } = "setnextmap";
    public string Extend { get; set; } = "ext,extendmap";
    public string MapsVote { get; set; } = "mapsvote";
    public string ChangeMap { get; set; } = "map,setmap";
    public string MapList { get; set; } = "maplist,maps";
    public string AddMap { get; set; } = "addmap";
    public string RemoveMap { get; set; } = "removemap";
    public string CycleMenu { get; set; } = "cyclemenu,mapcycle";
}

public class MapChooserConfig
{
    public RtvConfig Rtv { get; set; } = new();
    public VotemapConfig Votemap { get; set; } = new();
    public EndOfMapConfig EndOfMap { get; set; } = new();
    public ExtendMapConfig ExtendMap { get; set; } = new();
    public CycleConfig Cycle { get; set; } = new();
    public int MapsInCooldown { get; set; } = 3;
    public CommandsConfig Commands { get; set; } = new();
    public bool AllowSpectatorsToVote { get; set; } = false;
    public bool AnnounceVotes { get; set; } = true;
    public bool DisableVoteMenuExit { get; set; } = false;
    public bool DetailedLogging { get; set; } = false;
    public string SetNextMapPermission { get; set; } = "admin.changemap";
    public string MapsVotePermission { get; set; } = "admin.mapsvote";
    public string ChangeMapPermission { get; set; } = "admin.changemap";
}

public class MapsConfig
{
    public List<Map> Maps { get; set; } = new();
}
