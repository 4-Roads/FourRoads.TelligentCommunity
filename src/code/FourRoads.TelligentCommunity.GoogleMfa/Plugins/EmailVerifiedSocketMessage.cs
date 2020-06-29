using FourRoads.TelligentCommunity.Mfa.Interfaces;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Sockets.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.Mfa.Plugins
{
    public class EmailVerifiedSocketMessage :  ISocket, ISocketMessage
    {
        private IScriptedContentFragmentController _controller;
        private ISocketMessageBus _smb;
        private ISocketController _sockets;

        public void SetController(ISocketController controller)
        {
            _sockets = controller;
        }

        public string SocketName => "emailverify";

        #region PluginBase
        public string Description => "Messages to notify that email address has been verified";

        public void Initialize()
        {

        }

        public void NotifyCodeAccepted(User user)
        {
            if (_sockets != null)
            {
                _sockets.Clients.Send(user.Id.Value,"completed");
            }
        }
        public string Name => "Email Verified Socket Message";
        #endregion

    }
}