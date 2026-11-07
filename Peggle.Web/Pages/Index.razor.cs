using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Peggle.Web.Pages
{
    /// <summary>
    /// The page hosting the game. The saves are restored from the browser before anything starts; the game is
    /// created after the player clicks the start overlay (wwwroot/js/peggle.js), so browsers let it play sound,
    /// then runs one Tick per animation frame.
    /// </summary>
    public partial class Index
    {
        private Sexy.Main mGame;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (!firstRender)
            {
                return;
            }
            byte[] aSaves = await JsRuntime.InvokeAsync<byte[]>("peggleWeb.loadSaves");
            WebSaves.Restore(aSaves);
            await JsRuntime.InvokeVoidAsync("peggleWeb.ready", DotNetObjectReference.Create(this));
        }

        [JSInvokable]
        public void TickDotNet()
        {
            IJSInProcessRuntime aJs = (IJSInProcessRuntime)JsRuntime;
            if (mGame == null)
            {
                mGame = WebHost.CreateGame(aJs);
                mGame.Run();
            }
            mGame.Tick();
            WebSaves.Poll(aJs);
        }

        /// <summary>The page is being hidden or closed: saves are written now.</summary>
        [JSInvokable]
        public void FlushSaves()
        {
            WebSaves.Flush((IJSInProcessRuntime)JsRuntime);
        }
    }
}
