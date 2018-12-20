﻿using Dapper;
using IdentityServer4.Dapper.Interfaces;
using IdentityServer4.Dapper.Mappers;
using IdentityServer4.Dapper.Options;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Distributed;

namespace IdentityServer4.Dapper.DefaultProviders
{
    /// <summary>
    /// default client provider, which provide the query method and add or update
    /// </summary>
    public class DefaultClientProvider : IClientProvider
    {
        /// <summary>
        /// dbconfig options should be configed in each instance of db
        /// </summary>
        private DBProviderOptions _options;
        private readonly ILogger<DefaultClientProvider> _logger;

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="dBProviderOptions">db config options</param>
        /// <param name="logger">the logger</param>
        public DefaultClientProvider(DBProviderOptions dBProviderOptions, ILogger<DefaultClientProvider> logger)
        {
            this._options = dBProviderOptions ?? throw new ArgumentNullException(nameof(dBProviderOptions));
            this._logger = logger;
        }

        /// <summary>
        /// find client by client id.
        /// <para>make this method virtual for override in subclass.</para>
        /// </summary>
        /// <param name="clientid"></param>
        /// <returns></returns>
        public virtual Client FindClientById(string clientid)
        {
            var client = GetById(clientid);
            if (client == null)
            {
                return null;
            }

            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;

                if (client != null)
                {
                    //do not use the mutiquery in case of some db can not return muti sets
                    //if you want to redurce the time cost,please recode in your own class which should inherit from IClientProvider or this
                    var granttypes = GetClientGrantTypeByClientID(client.Id);
                    var redirecturls = GetClientRedirectUriByClientID(client.Id);
                    var postlogoutredirecturis = GetClientPostLogoutRedirectUriByClientID(client.Id);
                    var allowedscopes = GetClientScopeByClientID(client.Id);
                    var secrets = GetClientSecretByClientID(client.Id);
                    var claims = GetClientClaimByClientID(client.Id);
                    var iprestrictions = GetClientIdPRestrictionByClientID(client.Id);
                    var corsOrigins = GetClientCorsOriginByClientID(client.Id);
                    var properties = GetClientPropertyByClientID(client.Id);

                    if (granttypes != null)
                    {
                        foreach (var item in granttypes)
                        {
                            item.Client = client;
                        }
                        client.AllowedGrantTypes = granttypes.AsList();
                    }
                    if (redirecturls != null)
                    {
                        foreach (var item in redirecturls)
                        {
                            item.Client = client;
                        }
                        client.RedirectUris = redirecturls.AsList();
                    }

                    if (postlogoutredirecturis != null)
                    {
                        foreach (var item in postlogoutredirecturis)
                        {
                            item.Client = client;
                        }
                        client.PostLogoutRedirectUris = postlogoutredirecturis.AsList();
                    }
                    if (allowedscopes != null)
                    {
                        foreach (var item in allowedscopes)
                        {
                            item.Client = client;
                        }
                        client.AllowedScopes = allowedscopes.AsList();
                    }
                    if (secrets != null)
                    {
                        foreach (var item in secrets)
                        {
                            item.Client = client;
                        }
                        client.ClientSecrets = secrets.AsList();
                    }
                    if (claims != null)
                    {
                        foreach (var item in claims)
                        {
                            item.Client = client;
                        }
                        client.Claims = claims.AsList();
                    }
                    if (iprestrictions != null)
                    {
                        foreach (var item in iprestrictions)
                        {
                            item.Client = client;
                        }
                        client.IdentityProviderRestrictions = iprestrictions.AsList();
                    }
                    if (corsOrigins != null)
                    {
                        foreach (var item in corsOrigins)
                        {
                            item.Client = client;
                        }
                        client.AllowedCorsOrigins = corsOrigins.AsList();
                    }

                    if (properties != null)
                    {
                        foreach (var item in properties)
                        {
                            item.Client = client;
                        }
                        client.Properties = properties.AsList();
                    }
                }

                return client?.ToModel();
            }
        }

        public Entities.Client GetById(string clientid)
        {
            if (string.IsNullOrWhiteSpace(clientid))
            {
                return null;
            }

            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;

                return connection.QueryFirstOrDefault<Entities.Client>("select * from Clients where ClientId = @ClientId", new { ClientId = clientid }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);

            }
        }

        /// <summary>
        /// add the client to db.
        /// <para>clientid will be checked as unique key.</para> 
        /// </summary>
        /// <param name="client"></param>
        public void Add(Client client)
        {
            var dbclient = GetById(client.ClientId);
            if (dbclient != null)
            {
                throw new InvalidOperationException($"you can not add an existed client,clientid={client.ClientId}.");
            }
            var entity = client.ToEntity();
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        var ClientId = con.ExecuteScalar<int>($"insert into Clients (AbsoluteRefreshTokenLifetime,AccessTokenLifetime,AccessTokenType,AllowAccessTokensViaBrowser,AllowOfflineAccess,AllowPlainTextPkce,AllowRememberConsent,AlwaysIncludeUserClaimsInIdToken,AlwaysSendClientClaims,AuthorizationCodeLifetime,BackChannelLogoutSessionRequired,BackChannelLogoutUri,ClientClaimsPrefix,ClientId,ClientName,ClientUri,ConsentLifetime,Description,EnableLocalLogin,Enabled,FrontChannelLogoutSessionRequired,FrontChannelLogoutUri,IdentityTokenLifetime,IncludeJwtId,LogoUri,PairWiseSubjectSalt,ProtocolType,RefreshTokenExpiration,RefreshTokenUsage,RequireClientSecret,RequireConsent,RequirePkce,SlidingRefreshTokenLifetime,UpdateAccessTokenClaimsOnRefresh) values (@AbsoluteRefreshTokenLifetime,@AccessTokenLifetime,@AccessTokenType,@AllowAccessTokensViaBrowser,@AllowOfflineAccess,@AllowPlainTextPkce,@AllowRememberConsent,@AlwaysIncludeUserClaimsInIdToken,@AlwaysSendClientClaims,@AuthorizationCodeLifetime,@BackChannelLogoutSessionRequired,@BackChannelLogoutUri,@ClientClaimsPrefix,@ClientId,@ClientName,@ClientUri,@ConsentLifetime,@Description,@EnableLocalLogin,@Enabled,@FrontChannelLogoutSessionRequired,@FrontChannelLogoutUri,@IdentityTokenLifetime,@IncludeJwtId,@LogoUri,@PairWiseSubjectSalt,@ProtocolType,@RefreshTokenExpiration,@RefreshTokenUsage,@RequireClientSecret,@RequireConsent,@RequirePkce,@SlidingRefreshTokenLifetime,@UpdateAccessTokenClaimsOnRefresh);{_options.GetLastInsertID}", entity, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                        var ret = 0;
                        if (entity.AllowedGrantTypes != null)
                        {
                            foreach (var item in entity.AllowedGrantTypes)
                            {
                                ret = con.Execute("insert into ClientGrantTypes (ClientId,GrantType) values (@ClientId,@GrantType)", new
                                {
                                    ClientId,
                                    item.GrantType
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }

                        if (entity.RedirectUris != null)
                        {
                            foreach (var item in entity.RedirectUris)
                            {
                                ret = con.Execute("insert into ClientRedirectUris (ClientId,RedirectUri) values (@ClientId,@RedirectUri)", new
                                {
                                    ClientId,
                                    item.RedirectUri
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.PostLogoutRedirectUris != null)
                        {
                            foreach (var item in entity.PostLogoutRedirectUris)
                            {
                                ret = con.Execute("insert into ClientPostLoutRedirectUris (ClientId,PostLogoutRedirectUri) values (@ClientId,@PostLogoutRedirectUri)", new
                                {
                                    ClientId,
                                    item.PostLogoutRedirectUri
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.AllowedScopes != null)
                        {
                            foreach (var item in entity.AllowedScopes)
                            {
                                ret = con.Execute("insert into ClientScopes (ClientId,Scope) values (@ClientId,@Scope)", new
                                {
                                    ClientId,
                                    item.Scope
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.ClientSecrets != null)
                        {
                            foreach (var item in entity.ClientSecrets)
                            {
                                ret = con.Execute("insert into ClientSecrets (ClientId,Description,Expiration,Type,Value) values (@ClientId,@Description,@Expiration,@Type,@Value)", new
                                {
                                    ClientId,
                                    item.Description,
                                    item.Expiration,
                                    item.Type,
                                    item.Value
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.Claims != null)
                        {
                            foreach (var item in entity.Claims)
                            {
                                ret = con.Execute("insert into ClientClaims (ClientId,Type,Value) values (@ClientId,@Type,@Value)", new
                                {
                                    ClientId,
                                    item.Type,
                                    item.Value
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.IdentityProviderRestrictions != null)
                        {
                            foreach (var item in entity.IdentityProviderRestrictions)
                            {
                                ret = con.Execute("insert into ClientIdPRestrictions (ClientId,Provider) values (@ClientId,@Provider)", new
                                {
                                    ClientId,
                                    item.Provider,
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.AllowedCorsOrigins != null)
                        {
                            foreach (var item in entity.AllowedCorsOrigins)
                            {
                                ret = con.Execute("insert into ClientCorsOrigins (ClientId,Origin) values (@ClientId,@Origin)", new
                                {
                                    ClientId,
                                    item.Origin,
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.Properties != null)
                        {
                            string left = _options.ColumnProtect["left"];
                            string right = _options.ColumnProtect["right"];
                            foreach (var item in entity.Properties)
                            {
                                ret = con.Execute($"insert into ClientProperties (ClientId,{left}Key{right},{left}Value{right}) values (@ClientId,@Key,@Value)", new
                                {
                                    ClientId,
                                    item.Key,
                                    item.Value
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public IEnumerable<string> QueryAllowedCorsOrigins()
        {
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;
                var corsOrigins = connection.Query<string>("select distinct Origin from ClientCorsOrigins where Origin is not null", commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                return corsOrigins;
            }
        }

        public IEnumerable<Client> Search(string keywords, int pageIndex, int pageSize, out int totalCount)
        {
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;

                DynamicParameters pairs = new DynamicParameters();
                pairs.Add("keywords", "%" + keywords + "%");

                var countsql = "select count(1) from Clients where ClientId like @keywords or ClientName like @keywords";
                totalCount = connection.ExecuteScalar<int>(countsql, pairs, commandType: CommandType.Text);

                if (totalCount == 0)
                {
                    return null;
                }

                var clients = connection.Query<Entities.Client>(_options.GetPageQuerySQL("select * from Clients where ClientId like @keywords or ClientName like @keywords", pageIndex, pageSize, totalCount, "", pairs), pairs, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                if (clients != null)
                {
                    return clients.Select(c => c.ToModel());
                }
                return null;
            }
        }

        public void Remove(string clientid)
        {
            var cliententity = GetById(clientid);
            if (cliententity == null)
            {
                return;
            }
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        var ret = con.Execute($"delete from Clients where id=@id", new { cliententity.Id }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                        ret = con.Execute("delete from ClientGrantTypes where ClientId=@ClientId;" +
                            "delete from ClientRedirectUris where ClientId=@ClientId;" +
                            "delete from ClientPostLoutRedirectUris where ClientId=@ClientId;" +
                            "delete from ClientScopes where ClientId=@ClientId;" +
                            "delete from ClientSecrets where ClientId=@ClientId;" +
                            "delete from ClientClaims where ClientId=@ClientId;" +
                            "delete from ClientIdPRestrictions where ClientId=@ClientId;" +
                            "delete from ClientCorsOrigins where ClientId=@ClientId;" +
                            "delete from ClientProperties where ClientId=@ClientId;", new
                            {
                                ClientId = cliententity.Id
                            }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
            }
        }

        #region 子属性
        public IEnumerable<Entities.ClientGrantType> GetClientGrantTypeByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return con.Query<Entities.ClientGrantType>("select * from ClientGrantTypes where  ClientId = @ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
            }
        }

        public IEnumerable<Entities.ClientRedirectUri> GetClientRedirectUriByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return con.Query<Entities.ClientRedirectUri>("select * from ClientRedirectUris where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
            }
        }

        public IEnumerable<Entities.ClientPostLogoutRedirectUri> GetClientPostLogoutRedirectUriByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return con.Query<Entities.ClientPostLogoutRedirectUri>("select * from ClientPostLoutRedirectUris where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
            }
        }

        public IEnumerable<Entities.ClientScope> GetClientScopeByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return con.Query<Entities.ClientScope>("select * from ClientScopes where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
            }
        }

        public IEnumerable<Entities.ClientSecret> GetClientSecretByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return con.Query<Entities.ClientSecret>("select * from ClientSecrets where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
            }
        }
        public IEnumerable<Entities.ClientClaim> GetClientClaimByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return con.Query<Entities.ClientClaim>("select * from ClientClaims where ClientId=@ClientId", new
                {
                    ClientId = ClientId
                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
            }
        }

        public IEnumerable<Entities.ClientIdPRestriction> GetClientIdPRestrictionByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return con.Query<Entities.ClientIdPRestriction>("select * from ClientIdPRestrictions where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
            }
        }

        public IEnumerable<Entities.ClientCorsOrigin> GetClientCorsOriginByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return con.Query<Entities.ClientCorsOrigin>("select * from ClientCorsOrigins where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
            }
        }

        public IEnumerable<Entities.ClientProperty> GetClientPropertyByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return con.Query<Entities.ClientProperty>("select * from ClientProperties where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
            }
        }


        #endregion
    }
}
