using System.Web.Mvc;
namespace UpshotHelper.Helpers
{
    public static class UpshotExtensions
    {
        /// <summary>
        /// Configure the Upshot Context.
        /// </summary>
        /// <param name="htmlHelper">The <seealso cref="HtmlHelper"/>.</param>
        /// <returns>Returns an <seealso cref="UpshotConfigBuild"/>.</returns>
        public static UpshotConfigBuilder UpshotContext(this HtmlHelper htmlHelper)
        {
            return htmlHelper.UpshotContext(false);
        }

        /// <summary>
        /// Configure the Upshot Context.
        /// </summary>
        /// <param name="htmlHelper">The <seealso cref="HtmlHelper"/>.</param>
        /// <param name="bufferChanges">Configure Upshot to buffer changes.</param>
        /// <returns>Returns an <seealso cref="UpshotConfigBuild"/>.</returns>
        public static UpshotConfigBuilder UpshotContext(this HtmlHelper htmlHelper, bool bufferChanges)
        {
            return new UpshotConfigBuilder(htmlHelper, bufferChanges);
        }
    }
}
