using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class FullToken
    {
        public string AcessToken;
        public DateTimeOffset AccessTokenExpiration;
        public string RefreshToken;

        public FullToken() {;}

        public FullToken(string accessToken, DateTimeOffset accessTokenExpiration, string refreshToken)
        {
            AcessToken = accessToken;
            AccessTokenExpiration = accessTokenExpiration;
            RefreshToken = refreshToken;
        }
    }
}
