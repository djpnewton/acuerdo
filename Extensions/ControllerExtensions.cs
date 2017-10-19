namespace Microsoft.AspNetCore.Mvc
{
   public static class FlashMessages
    {
        public static string Info = "InfoMessage";
        public static string Success = "SuccessMessage";
        public static string Error = "ErrorMessage";
        public static string Warning = "WarningMessage";
    }

    //TODO: allow mulitple flashes of same type???
    public static class ControllerExtensions
    {
        static void FlashMessage(this Controller controller, string type, string message)
        {
            controller.TempData[type] = message;
        }
        public static void FlashInfo(this Controller controller, string message)
        {
            FlashMessage(controller, FlashMessages.Info, message);
        }

        public static void FlashSuccess(this Controller controller, string message)
        {
            FlashMessage(controller, FlashMessages.Success, message);
        }

        public static void FlashError(this Controller controller, string message)
        {
            FlashMessage(controller, FlashMessages.Error, message);
        }

        public static void FlashWarning(this Controller controller, string message)
        {
            FlashMessage(controller, FlashMessages.Warning, message);
        }
    }
 }
