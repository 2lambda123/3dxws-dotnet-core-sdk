using ds.authentication;
using ds.authentication.redirection;
using ds.enovia.common.collection;
using ds.enovia.common.model;
using ds.enovia.common.search;
using ds.enovia.dsxcad.exception;
using ds.enovia.dsxcad.model;
using ds.enovia.dsxcad.service;
using ds.enovia.model;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ds.enovia.dsxcad.tests
{
    public class Tests
    {
        const string DS3DXWS_AUTH_USERNAME = "DS3DXWS_AUTH_USERNAME";
        const string DS3DXWS_AUTH_PASSWORD = "DS3DXWS_AUTH_PASSWORD";
        const string DS3DXWS_AUTH_PASSPORT = "DS3DXWS_AUTH_PASSPORT";
        const string DS3DXWS_AUTH_ENOVIA   = "DS3DXWS_AUTH_ENOVIA";
        const string DS3DXWS_AUTH_TENANT   = "DS3DXWS_AUTH_TENANT";

        string m_username    = string.Empty;
        string m_password    = string.Empty;
        string m_passportUrl = string.Empty;
        string m_enoviaUrl   = string.Empty;
        string m_tenant      = string.Empty;

        UserInfo m_userInfo = null;

        [SetUp]
        public void Setup()
        {
            m_username = Environment.GetEnvironmentVariable(DS3DXWS_AUTH_USERNAME, EnvironmentVariableTarget.User); // e.g. AAA27
            m_password = Environment.GetEnvironmentVariable(DS3DXWS_AUTH_PASSWORD, EnvironmentVariableTarget.User); // e.g. your password
            m_passportUrl = Environment.GetEnvironmentVariable(DS3DXWS_AUTH_PASSPORT, EnvironmentVariableTarget.User); // e.g. https://eu1-ds-iam.3dexperience.3ds.com:443/3DPassport

            m_enoviaUrl = Environment.GetEnvironmentVariable(DS3DXWS_AUTH_ENOVIA, EnvironmentVariableTarget.User); // e.g. https://r1132100982379-eu1-space.3dexperience.3ds.com:443/enovia
            m_tenant = Environment.GetEnvironmentVariable(DS3DXWS_AUTH_TENANT, EnvironmentVariableTarget.User); // e.g. R1132100982379
        }

        public async Task<IPassportAuthentication> Authenticate()
        {
            UserPassport passport = new UserPassport(m_passportUrl);

            UserInfoRedirection userInfoRedirection = new UserInfoRedirection(m_enoviaUrl, m_tenant);
            userInfoRedirection.Current = true;
            userInfoRedirection.IncludeCollaborativeSpaces = true;
            userInfoRedirection.IncludePreferredCredentials = true;

            m_userInfo = await passport.CASLoginWithRedirection<UserInfo>(m_username, m_password, false, userInfoRedirection);

            Assert.IsNotNull(m_userInfo);

            StringAssert.AreEqualIgnoringCase(m_userInfo.name, m_username);

            Assert.IsTrue(passport.IsCookieAuthenticated);

            return passport;
        }

      public string GetDefaultSecurityContext(enovia.model.UserInfo _userInfo)
      {
         SecurityContext __securityContext = new SecurityContext();

         __securityContext.collabspace  = _userInfo.preferredcredentials.collabspace;
         __securityContext.organization = _userInfo.preferredcredentials.organization;
         __securityContext.role         = _userInfo.preferredcredentials.role;

         return __securityContext.ToString();
      }

      public async Task<IPassportAuthentication> AuthenticateOnPremise()
      {
         UserPassport passport = new UserPassport(m_passportUrl);

         UserInfoRedirection userInfoRedirection = new UserInfoRedirection(m_enoviaUrl);
         userInfoRedirection.Current = true;
         userInfoRedirection.IncludeCollaborativeSpaces = true;
         userInfoRedirection.IncludePreferredCredentials = true;

         m_userInfo = await passport.CASLoginWithRedirection<UserInfo>(m_username, m_password, false, userInfoRedirection);

         Assert.IsNotNull(m_userInfo);

         StringAssert.AreEqualIgnoringCase(m_userInfo.name, m_username);

         Assert.IsTrue(passport.IsCookieAuthenticated);

         return passport;
      }

      [TestCase("VPLMAdmin.Company Name.Default", "c:\\temp", "xcadmodel-R1132100982379-00000190", "A.1", 180)]
        public async Task Download_XCADModelAuthoringFile(string _securityContext, string _downloadPath, string _cadfamilymodelName, string _cadfamilymodelRevision, int _timeout)
        {
            #region Arrange

            Assert.IsTrue(Directory.Exists(_downloadPath), $"Cannot find '{_downloadPath}'");

            //Authenticate
            IPassportAuthentication passport = await Authenticate();

            //Search for the xCAD Family by name and revision
            xCADFamilyRepresentationService xcadService = new xCADFamilyRepresentationService(m_enoviaUrl, passport);
            xcadService.SecurityContext = _securityContext;
            xcadService.Tenant = m_tenant;

            SearchByNameRevision query = new SearchByNameRevision(_cadfamilymodelName, _cadfamilymodelRevision);

            IList<xCADFamilyRepresentation> searchReturnSet = await xcadService.Search(query);

            Assert.IsNotNull(searchReturnSet);
            Assert.NotZero(searchReturnSet.Count, $"Cannot find XCADFamilyRepresentation with name = '{_cadfamilymodelName}' and revision = '{_cadfamilymodelRevision}'");

            xCADFamilyRepresentation part = searchReturnSet[0];
            string partId = part.id;
            
            HttpClient downloadHttpClient = new ServiceCollection().AddHttpClient().BuildServiceProvider().GetService<IHttpClientFactory>().CreateClient("download");

            downloadHttpClient.Timeout = TimeSpan.FromSeconds(_timeout);
            #endregion

            #region Act - download authoring file
            FileInfo downloadedFile = await xcadService.DownloadAuthoringFile(downloadHttpClient, partId, _downloadPath);
            #endregion

            #region Assert
            Assert.IsNotNull(downloadedFile);
            #endregion
        }
      [TestCase("VPLMAdmin.Company Name.Default", "AAA27 Personal")]
      public async Task Search_Products_And_Get_Details_of_First(string _securityContext, string _collaborativeSpace)
      {
        
         //Authenticate
         IPassportAuthentication passport = await Authenticate();

         //Search for the xCAD Family by name and revision
         xCADProductService xcadService = new xCADProductService(m_enoviaUrl, passport);
         xcadService.SecurityContext = _securityContext;
         xcadService.Tenant = m_tenant;

         SearchByCollaborativeSpace query = new SearchByCollaborativeSpace(_collaborativeSpace);

         IList<xCADProduct> searchReturnSet = await xcadService.Search(query);

         Assert.IsNotNull(searchReturnSet);
         Assert.NotZero(searchReturnSet.Count, $"Cannot find Products in Collaborative Space'{_collaborativeSpace}'");

         xCADProduct product = searchReturnSet[0];
         string productId = product.id;

         xCADProduct productDetails  = await xcadService.GetXCADProduct(productId, xCADProductDetails.Details);

         #region Assert
         Assert.IsNotNull(productDetails);

         Assert.AreEqual(productId, productDetails.id);
         #endregion
      }

      [TestCase("VPLMAdmin.Company Name.Default", "AAA27 Personal")]
      public async Task Search_Parts_And_Get_Details_of_First(string _securityContext, string _collaborativeSpace)
      {

         //Authenticate
         IPassportAuthentication passport = await Authenticate();

         //Search for the xCAD Family by name and revision
         xCADPartService xcadService = new xCADPartService(m_enoviaUrl, passport);
         xcadService.SecurityContext = _securityContext;
         xcadService.Tenant = m_tenant;

         SearchByCollaborativeSpace query = new SearchByCollaborativeSpace(_collaborativeSpace);

         IList<xCADPart> searchReturnSet = await xcadService.Search(query);

         Assert.IsNotNull(searchReturnSet);
         Assert.NotZero(searchReturnSet.Count, $"Cannot find Products in Collaborative Space'{_collaborativeSpace}'");

         xCADPart part = searchReturnSet[0];
         string partId = part.id;

         xCADPart partDetails = await xcadService.GetXCADPart(partId, xCADPartDetails.Details);

         #region Assert
         Assert.IsNotNull(partDetails);

         Assert.AreEqual(partId, partDetails.id);

         Assert.IsNotNull(partDetails.AuthoringFile);
         #endregion
      }

      [TestCase("VPLMAdmin.Company Name.Default", "AAA27 Personal")]
      public async Task Search_Representations_And_Get_Details_of_First(string _securityContext, string _collaborativeSpace)
      {

         //Authenticate
         IPassportAuthentication passport = await Authenticate();

         //Search for the xCAD Family by name and revision
         xCADRepresentationService xcadService = new xCADRepresentationService(m_enoviaUrl, passport);
         xcadService.SecurityContext = _securityContext;
         xcadService.Tenant = m_tenant;

         SearchByCollaborativeSpace query = new SearchByCollaborativeSpace(_collaborativeSpace);

         IList<xCADRepresentation> searchReturnSet = await xcadService.Search(query);

         Assert.IsNotNull(searchReturnSet);
         Assert.NotZero(searchReturnSet.Count, $"Cannot find Representations in Collaborative Space'{_collaborativeSpace}'");

         xCADRepresentation rep = searchReturnSet[0];
         string repId = rep.id;

         xCADRepresentation repDetails = await xcadService.GetXCADRepresentation(repId, xCADRepresentationDetails.Details);

         #region Assert
         Assert.IsNotNull(repDetails);

         Assert.AreEqual(repId, repDetails.id);
         #endregion
      }


      [TestCase("VPLMAdmin.Company Name.Default", "AAA27 Personal")]
      public async Task Search_Templates_And_Get_Details_of_First(string _securityContext, string _collaborativeSpace)
      {

         //Authenticate
         IPassportAuthentication passport = await Authenticate();

         //Search for the xCAD Family by name and revision
         xCADTemplateService xcadService = new xCADTemplateService(m_enoviaUrl, passport);
         xcadService.SecurityContext = _securityContext;
         xcadService.Tenant = m_tenant;

         SearchByCollaborativeSpace query = new SearchByCollaborativeSpace(_collaborativeSpace);

         IList<xCADTemplate> searchReturnSet = await xcadService.Search(query);

         Assert.IsNotNull(searchReturnSet);
         Assert.NotZero(searchReturnSet.Count, $"Cannot find Templates in Collaborative Space'{_collaborativeSpace}'");

         xCADTemplate template = searchReturnSet[0];
         string templateId = template.id;

         xCADTemplate templateDetails = await xcadService.GetXCADTemplate(templateId, xCADTemplateDetails.Default);

         #region Assert
         Assert.IsNotNull(templateDetails);

         Assert.AreEqual(templateId, templateDetails.id);
         #endregion
      }


      [TestCase("VPLMAdmin.Company Name.Default", "AAA27 Personal")]
      public async Task LocatexCADDrawing(string _securityContext, string _collaborativeSpace)
      {

         //Authenticate
         IPassportAuthentication passport = await Authenticate();

         //Search for the xCAD Family by name and revision
         BusinessObjectId id1 = new BusinessObjectId();

         id1.id = "79DD880FF25300005F7F0CEA000E4CE1";
         id1.type = "dseng:EngItem";
         id1.source = "$3DSpace";
         id1.relativePath = "resource/v1/dseng/dseng:EngItem/79DD880FF25300005F7F0CEA000E4CE1";

         xCADDrawingService xcadService = new xCADDrawingService(m_enoviaUrl, passport);
         xcadService.SecurityContext = _securityContext;
         xcadService.Tenant = m_tenant;

         ItemSet<xCADDrawingReference> id1Response = await xcadService.Locate(id1);

         Assert.AreEqual(id1Response.totalItems, 1);

         //Search for the xCAD Family by name and revision
         BusinessObjectId id2 = new BusinessObjectId();

         id2.id = "1F53D9FDBA31000061570D8E00183A91";
         id2.type = "dsxcad:Part";
         id2.source = "$3DSpace";
         id2.relativePath = "resource/v1/dsxcad/dsxcad:Part/1F53D9FDBA31000061570D8E00183A91";

         ItemSet<xCADDrawingReference> id2Response = await xcadService.Locate(id2);

         Assert.AreEqual(id2Response.totalItems, 1);

         BusinessObjectId id3 = new BusinessObjectId();

         id3.id = "35B0B3DB487B00005F0388A60006EEB9";
         id3.type = "dsxcad:Part";
         id3.source = "$3DSpace";
         id3.relativePath = "resource/v1/dsxcad/dsxcad:Part/35B0B3DB487B00005F0388A60006EEB9";

         ItemSet<xCADDrawingReference> id3Response = await xcadService.Locate(id3);

         Assert.AreEqual(id3Response.totalItems, 0);
      }

      [TestCase("VPLMAdmin.Company Name.Default", "ATG - SaaS Readiness")]
      public async Task GetAndLocatexCADFamilyMembers(string _securityContext, string _collaborativeSpace)
      {
         //Authenticate
         IPassportAuthentication passport = await Authenticate();

         SearchByCollaborativeSpace query = new SearchByCollaborativeSpace(_collaborativeSpace);

         xCADFamilyRepresentationService xcadService = new xCADFamilyRepresentationService(m_enoviaUrl, passport);
         xcadService.SecurityContext = _securityContext;
         xcadService.Tenant = m_tenant;

         IList<xCADFamilyRepresentation> searchReturnSet = await xcadService.Search(query);

         IList<string> noMemberCADFamilyList = new List<string>();

         double total = searchReturnSet.Count;
         double iCt = 0;
         foreach (xCADFamilyRepresentation family in searchReturnSet)
         {
            //Console.WriteLine($"Analyzing cad family with id = {family.id}");
            xCADFamilyRepresentation cadFamilyDetails = await xcadService.GetXCADFamilyRepresentation(family.id, xCADFamilyRepresentationDetails.Details);
            Assert.IsNotNull(cadFamilyDetails.cadtype);

            if (cadFamilyDetails.DerivedItems != null)
            {
               Console.WriteLine($"DerivedItems.totalItems={cadFamilyDetails.DerivedItems.totalItems}");

               foreach (xCADFamilyMember familyMember in cadFamilyDetails.DerivedItems.member)
               {
                  MemberSet<xCADFamilyRepresentationReference> refIdResponse = await xcadService.Locate(familyMember.referencedObject);

                  Assert.IsNotNull(refIdResponse);
                  Assert.IsNotNull(refIdResponse.member);
                  Assert.AreEqual(refIdResponse.member.Count, 1);
                  Assert.IsNotNull(refIdResponse.member[0]);
                  Assert.IsNotNull(refIdResponse.member[0].representation);
                  Assert.IsNotNull(refIdResponse.member[0].representation.identifier);

                  Assert.AreEqual(refIdResponse.member[0].representation.identifier, cadFamilyDetails.id);
               }
            }
            else
            {
               noMemberCADFamilyList.Add(cadFamilyDetails.id);
               Console.WriteLine($"DerivedItems.totalItems=0");
            }

            iCt++;

            Thread.Sleep(50);
            //if (iCt > 20)
            //{
            //   break;
            //}
         }

         Console.WriteLine($"A total of {noMemberCADFamilyList.Count} CAD Families without members, out of a total of {searchReturnSet.Count}.");

      }

      [TestCase("VPLMAdmin.Company Name.Default", "ATG - SaaS Readiness")]
      public async Task SearchPartItems(string _securityContext, string _collaborativeSpace)
      {
         //Authenticate
         IPassportAuthentication passport = await Authenticate();

         SearchByFreeText query = new SearchByFreeText("([ds6w: modified] >= \"2022-09-24T09:12:50.7891897Z\" AND [ds6w: modified] <= \"2022-09-26T09:12:50.7891897Z\") AND ([ds6w: status]:(VPLM_SMB_Definition.FROZEN)) AND(project:\"AAA27 Personal\" OR project:\"ATG - SaaS Readiness\" OR project: \"CMU\")");
         //SearchByCollaborativeSpace query = new SearchByCollaborativeSpace(_collaborativeSpace);

         xCADPartService xcadService = new xCADPartService(m_enoviaUrl, passport);
         xcadService.SecurityContext = _securityContext;
         xcadService.Tenant = m_tenant;

         IList<xCADPart> searchReturnSet = await xcadService.Search(query);

        double total = searchReturnSet.Count;
         double iCt = 0;


         Console.WriteLine($"A total of {searchReturnSet.Count} Parts have been returned.");

      }


      [TestCase("VPLMProjectLeader.Company Name.CS-PRS-4_vdemo299", "CS-PRS-4_vdemo299")]
      public async Task GetPartItems(string _securityContext, string _collaborativeSpace)
      {
         //Authenticate
         IPassportAuthentication passport = await AuthenticateOnPremise();

         //SearchByFreeText query = new SearchByFreeText("([ds6w: modified] >= \"2022-09-24T09:12:50.7891897Z\" AND [ds6w: modified] <= \"2022-09-26T09:12:50.7891897Z\") AND ([ds6w: status]:(VPLM_SMB_Definition.FROZEN)) AND(project:\"AAA27 Personal\" OR project:\"ATG - SaaS Readiness\" OR project: \"CMU\")");
         //SearchByCollaborativeSpace query = new SearchByCollaborativeSpace(_collaborativeSpace);

         xCADPartService xcadService = new xCADPartService(m_enoviaUrl, passport);
         xcadService.SecurityContext = _securityContext;
         xcadService.Tenant = m_tenant;

         xCADPart part = await xcadService.GetXCADPart("7091BF56189000006399F231001C977D", xCADPartDetails.Details);

         //Console.WriteLine($"A total of {searchReturnSet.Count} Parts have been returned.");

      }

      //Euromed tenant
      [TestCase( new object[] { "34149A024463000063C0BF05001C04F9", "22E1AA12E91B000063C7F85A001E5883" } )]
      //issue #52 - Extend CAD Family Locate with multiple object input 
      public async Task GetAndLocateMultiplexCADFamilyMembers(params string[] _partIdList)
      {
         //Authenticate
         IPassportAuthentication passport = await Authenticate();

         xCADFamilyRepresentationService xcadService = new xCADFamilyRepresentationService(m_enoviaUrl, passport);
         xcadService.SecurityContext = GetDefaultSecurityContext(m_userInfo);
         xcadService.Tenant = m_tenant;

         IList<string> noMemberCADFamilyList = new List<string>();

         IList<BusinessObjectIdentifier> businessObjectIds = new List<BusinessObjectIdentifier>();

         string errorMessage = "";
         MemberSet<xCADFamilyRepresentationReference> refIdResponse = null;

         foreach (string partId in _partIdList)
         {
            BusinessObjectIdentifier newBusinessObjectId = new BusinessObjectIdentifier();
            newBusinessObjectId.identifier = partId;
            newBusinessObjectId.source     = "$3DSpace";
            newBusinessObjectId.type       = "VPMReference";
            newBusinessObjectId.relativePath = "resource/v1/dsxcad/dsxcad:Part/" + partId;

            businessObjectIds.Add(newBusinessObjectId);
         }
         try
         {
            refIdResponse = await xcadService.Locate(businessObjectIds);
         }
         catch (LocateXCADFamilyException _ex)
         {
            errorMessage = await _ex.GetErrorMessage();
         }

         Assert.IsNotNull(refIdResponse);
         Assert.IsNotNull(refIdResponse.member);
         Assert.AreEqual(refIdResponse.member.Count, 2);

      }

   }
}