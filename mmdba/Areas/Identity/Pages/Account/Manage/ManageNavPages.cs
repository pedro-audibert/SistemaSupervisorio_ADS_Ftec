// Licenciado para a .NET Foundation sob um ou mais acordos.
// A .NET Foundation licencia este arquivo para você sob a licença MIT.
#nullable disable

using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace mmdba.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
    /// e não é destinada ao uso direto no seu código. 
    /// Esta API pode mudar ou ser removida em versões futuras.
    /// </summary>
    public static class ManageNavPages
    {
        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string Index => "Index";

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string Email => "Email";

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string ChangePassword => "ChangePassword";

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string DownloadPersonalData => "DownloadPersonalData";

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string DeletePersonalData => "DeletePersonalData";

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string ExternalLogins => "ExternalLogins";

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string PersonalData => "PersonalData";

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string TwoFactorAuthentication => "TwoFactorAuthentication";

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string EmailNavClass(ViewContext viewContext) => PageNavClass(viewContext, Email);

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string ChangePasswordNavClass(ViewContext viewContext) => PageNavClass(viewContext, ChangePassword);

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string DownloadPersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, DownloadPersonalData);

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string DeletePersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, DeletePersonalData);

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string ExternalLoginsNavClass(ViewContext viewContext) => PageNavClass(viewContext, ExternalLogins);

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string PersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, PersonalData);

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string TwoFactorAuthenticationNavClass(ViewContext viewContext) => PageNavClass(viewContext, TwoFactorAuthentication);

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        public static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string
                ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
        }
    }
}