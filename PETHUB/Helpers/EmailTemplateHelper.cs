namespace PETHUB.Helpers
{
    public static class EmailTemplateHelper
    {
        // ===========================
        // PASSWORD RESET EMAIL
        // ===========================
        public static string PasswordReset(string firstName, string resetLink)
        {
            return BuildTemplate(
                title: "Reset Your Password",
                heading: "Password Reset Request",
                message:
                    $"Hello {firstName},<br><br>" +
                    $"We received a request to reset the password for your PETHUB account.<br><br>" +
                    $"Click the button below to create a new password.<br><br>" +
                    $"<strong>This password reset link will expire in 10 minutes.</strong>",
                buttonText: "Reset Password",
                buttonLink: resetLink,
                footer:
                    "If you didn't request this password reset, you can safely ignore this email."
            );
        }

        // ===========================
        // EMAIL VERIFICATION
        // ===========================
        public static string EmailVerification(string firstName, string verificationLink)
        {
            return BuildTemplate(
                title: "Verify Your Email",
                heading: $"Welcome to PETHUB, {firstName}!",
                message:
                    "Thank you for creating an account.<br><br>" +
                    "Before you can start using PETHUB, " +
                    "please verify your email address.<br><br>" +
                    "<strong>This verification link will expire in 24 hours.</strong>",
                buttonText: "Verify Email",
                buttonLink: verificationLink,
                footer:
                    "If you did not create this account, you may safely ignore this email."
            );
        }

        // ===========================
        // WELCOME EMAIL
        // ===========================
        public static string Welcome(string firstName, string baseUrl)
        {
            return BuildTemplate(
                title: "Welcome to PETHUB",
                heading: $"Welcome, {firstName}!",
                message:
                    "Your account has been successfully created.<br><br>" +
                    "You can now explore the Marketplace, PetFeed, and Lost & Found features.",
                buttonText: "Visit PETHUB",
                buttonLink: baseUrl,
                footer:
                    "Thank you for joining the PETHUB community!"
            );
        }

        // ===========================
        // PASSWORD CHANGED
        // ===========================
        public static string PasswordChanged(string firstName, string loginLink)
        {
            return BuildTemplate(
                title: "Password Successfully Changed",
                heading: "Your Password Has Been Updated",
                message:
                    $"Hello {firstName},<br><br>" +
                    "Your PETHUB password was changed successfully.<br><br>" +
                    "If you made this change, no further action is required.",
                buttonText: "Login to PETHUB",
                buttonLink: loginLink,
                footer:
                    "If you did not perform this action, please contact PETHUB Support immediately."
            );
        }


        // ===========================
        // ADMIN INVITATION
        // ===========================
        public static string AdminInvitation(string invitationLink)
        {
            return BuildTemplate(
                title: "PETHUB Administrator Invitation",
                heading: "You're Invited to Become a PETHUB Administrator",
                message:
                    "You have been invited to create an administrator account for PETHUB.<br><br>" +
                    "Click the button below to complete your administrator account setup.<br><br>" +
                    "<strong>This invitation link is for you only. Please do not share it with anyone.</strong>",
                buttonText: "Create Administrator Account",
                buttonLink: invitationLink,
                footer:
                    "If you were not expecting this invitation, you can safely ignore this email."
            );
        }

        // ===========================
        // ADMIN-CREATED MEMBER
        // EMAIL VERIFICATION
        // ===========================

        public static string AdminCreatedMemberVerification(
            string firstName,
            string verificationLink)
        {
            return BuildTemplate(
                title: "Your PETHUB Account Has Been Created",
                heading: $"Welcome to PETHUB, {firstName}!",
                message:
                    "A PETHUB administrator has created a member account for you.<br><br>" +
                    "Please verify your email address by clicking the button below.<br><br>" +
                    "<strong>This verification link will expire in 24 hours.</strong><br><br>" +
                    "After verifying your email, we strongly recommend changing your password using the <strong>Forgot Password</strong> option on the PETHUB login page.",
                buttonText: "Verify Email",
                buttonLink: verificationLink,
                footer:
                    "If you were not expecting this account, please contact the PETHUB administrators."
            );
        }

        // ============================================================
        // MAIN TEMPLATE
        // ============================================================

        private static string BuildTemplate(
            string title,
            string heading,
            string message,
            string buttonText,
            string buttonLink,
            string footer)
                {
                    return $@"
            <!DOCTYPE html>

            <html>

            <head>

            <meta charset='UTF-8'>

            <title>{title}</title>

            </head>

            <body style='
            margin:0;
            padding:50px 20px;
            background:#F8FAF7;
            font-family:Segoe UI,Arial,sans-serif;'>

            <table
            align='center'
            width='620'
            style='
            background:#FFFFFF;
            border-radius:28px;
            overflow:hidden;
            box-shadow:0 12px 35px rgba(0,0,0,.08);'>

            <tr>

            <td style='padding:60px 55px;text-align:center;'>

            <span style='
            display:inline-block;
            background:#EAF8E6;
            color:#4F8D42;
            padding:8px 22px;
            border-radius:999px;
            font-size:13px;
            font-weight:600;
            letter-spacing:.5px;
            margin-bottom:28px;'>

            PETHUB

            </span>

            <h1 style='
            margin:0;
            font-size:34px;
            font-weight:700;
            line-height:1.3;
            color:#2D3748;'>

            {heading}

            </h1>

            <p style='
            margin:28px 0 40px;
            font-size:16px;
            line-height:1.9;
            color:#6B7280;'>

            {message}

            </p>

            <a href='{buttonLink}'
            style='
            display:inline-block;
            background:#8ACE79;
            color:white;
            text-decoration:none;
            padding:16px 42px;
            border-radius:999px;
            font-size:16px;
            font-weight:600;'>

            {buttonText}

            </a>

            </td>

            </tr>

            <tr>

            <td style='padding:0 55px;'>

            <hr style='border:none;border-top:1px solid #ECECEC;'>

            </td>

            </tr>

            <tr>

            <td style='padding:35px 55px;'>

            <p style='
            margin:0;
            font-size:15px;
            line-height:1.8;
            color:#666;'>

            {footer}

            </p>

            </td>

            </tr>

            <tr>

            <td style='
            background:#F8FAF7;
            padding:30px;
            text-align:center;'>

            <p style='
            margin:0;
            font-size:13px;
            color:#7A7A7A;
            line-height:1.8;'>

            Need assistance?

            <br>

            <strong style='color:#4F8D42;'>

            pethubofficialplatform@gmail.com

            </strong>

            </p>

            <p style='
            margin-top:24px;
            font-size:12px;
            color:#A5A5A5;'>

            © 2026 PETHUB

            <br>

            Helping every pet find a loving home.

            <br><br>

            This is an automated email. Please do not reply.

            </p>

            </td>

            </tr>

            </table>

            </body>

            </html>";
        }
    }
}