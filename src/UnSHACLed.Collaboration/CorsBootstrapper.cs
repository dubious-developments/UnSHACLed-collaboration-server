using System.Linq;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// A bootstrapper implementation that enables cross-origin resource sharing (CORS).
    /// </summary>
    public class CorsBootstrapper : DefaultNancyBootstrapper
    {
        /// <inheritdoc/>
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            EnableCors(pipelines);
        }

        /// <summary>
        /// Enables cross-origin resource sharing (CORS).
        /// </summary>
        /// <param name="pipelines">The pipelines to enable CORS for.</param>
        private static void EnableCors(IPipelines pipelines)
        {
            // Code adapted from Endy Tjahjono's answer to
            // https://stackoverflow.com/questions/15658627/is-it-possible-to-enable-cors-using-nancyfx#23554350

            pipelines.AfterRequest.AddItemToEndOfPipeline(ctx =>
            {
                if (ctx.Request.Headers.Keys.Contains("Origin"))
                {
                    var origins = "" + string.Join(" ", ctx.Request.Headers["Origin"]);
                    ctx.Response.Headers["Access-Control-Allow-Origin"] = origins;

                    if (ctx.Request.Method == "OPTIONS")
                    {
                        // Handle CORS preflight request.
                        ctx.Response.Headers["Access-Control-Allow-Methods"] =
                            "GET, POST, PUT, DELETE, OPTIONS";

                        if (ctx.Request.Headers.Keys.Contains("Access-Control-Request-Headers"))
                        {
                            var allowedHeaders = "" + string.Join(
                                ", ", ctx.Request.Headers["Access-Control-Request-Headers"]);
                            ctx.Response.Headers["Access-Control-Allow-Headers"] = allowedHeaders;
                        }
                    }
                }
            });
        }
    }
}