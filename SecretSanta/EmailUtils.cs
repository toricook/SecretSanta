using EASendMail;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;

namespace SecretSanta
{
    public static class EmailUtils
    {

        public static async Task<UserCredential> GetGmailCredentials(string clientId, string clientSecret)
        {
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    new[] { "email", "profile", "https://mail.google.com/" },
                    "user",
                    CancellationToken.None
                    );

            var jwtPayload = GoogleJsonWebSignature.ValidateAsync(credential.Token.IdToken).Result;
            var username = jwtPayload.Email;

            return credential;

        }
        public static void SendGmail(string userEmail, string accessToken, string recipient, string subject, string body)
        {
            try
            {
                // Gmail API server address
                SmtpServer oServer = new SmtpServer("https://www.googleapis.com/upload/gmail/v1/users/me/messages/send?uploadType=media");

                // set Gmail RESTFul API protocol 
                oServer.Protocol = ServerProtocol.GmailApi;

                // enable SSL connection
                oServer.ConnectType = SmtpConnectType.ConnectSSLAuto;

                // use Gmail OAUTH 2.0 authentication
                oServer.AuthType = SmtpAuthType.XOAUTH2;
                // set user authentication
                oServer.User = userEmail;
                // use access token as password
                oServer.Password = accessToken;

                SmtpMail oMail = new SmtpMail("TryIt");
                // Your gmail email address
                oMail.From = userEmail;
                oMail.To = recipient;

                oMail.Subject = subject;
                oMail.TextBody = body;

                Console.WriteLine($"Sending email to {recipient}...");

                SmtpClient oSmtp = new SmtpClient();
                oSmtp.SendMail(oServer, oMail);

                Console.WriteLine("The email has been submitted to the server successfully!");
            }
            catch (Exception ep)
            {
                Console.WriteLine("Exception: {0}", ep.Message);
            }
        }


    }
}
