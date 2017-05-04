namespace FourRoads.TelligentCommunity.Rules.Tokens
{
    /// <summary>
    /// Used to pass custom values to the rule config UI (conditions etc) and rule actions
    /// </summary>
    /// 
    public class UpVoteTriggerParameters
    {
        public int ReplyUpVotes { get; set; }
        public int ThreadUpVotes { get; set; }
    }
}
