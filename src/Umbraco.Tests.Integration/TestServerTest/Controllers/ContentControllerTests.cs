﻿using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Tests.Common.Builders;
using Umbraco.Tests.Common.Builders.Extensions;
using Umbraco.Tests.Testing;
using Umbraco.Web.Common.Formatters;
using Umbraco.Web.Editors;
using Umbraco.Web.Models.ContentEditing;

namespace Umbraco.Tests.Integration.TestServerTest.Controllers
{
    [TestFixture]
    [UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
    public class ContentControllerTests : UmbracoTestServerTestBase
    {
        [SetUp]
        public void Setup()
        {
            var localizationService = GetRequiredService<ILocalizationService>();

            //Add another language
            localizationService.Save(new LanguageBuilder()
                .WithCultureInfo("da-DK")
                .WithIsDefault(false)
                .Build());
        }

        /// <summary>
        ///     Returns 404 if the content wasn't found based on the ID specified
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task PostSave_Validate_Existing_Content()
        {
            var url = PrepareUrl<ContentController>(x => x.PostSave(null));

            var contentService = GetRequiredService<IContentService>();
            var contentTypeService = GetRequiredService<IContentTypeService>();

            var contentType = new ContentTypeBuilder()
                .WithId(0)
                .AddPropertyType()
                .WithAlias("title")
                .WithValueStorageType(ValueStorageType.Integer)
                .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
                .WithName("Title")
                .Done()
                .WithContentVariation(ContentVariation.Nothing)
                .Build();

            contentTypeService.Save(contentType);

            var content = new ContentBuilder()
                .WithId(0)
                .WithName("Invariant")
                .WithContentType(contentType)
                .AddPropertyData()
                .WithKeyValue("title", "Cool invariant title")
                .Done()
                .Build();
            contentService.SaveAndPublish(content);

            var model = new ContentItemSaveBuilder()
                .WithContent(content)
                .WithId(-1337) // HERE We overwrite the Id, so we don't expect to find it on the server
                .Build();

            // Act
            var response = await Client.PostAsync(url, new MultipartFormDataContent
            {
                { new StringContent(JsonConvert.SerializeObject(model)), "contentItem" }
            });

            // Assert

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
//             Assert.AreEqual(")]}',\n{\"Message\":\"content was not found\"}", response.Item1.Content.ReadAsStringAsync().Result);
//
//             //var obj = JsonConvert.DeserializeObject<PagedResult<UserDisplay>>(response.Item2);
//             //Assert.AreEqual(0, obj.TotalItems);
        }

        [Test]
        public async Task PostSave_Validate_At_Least_One_Variant_Flagged_For_Saving()
        {
            var url = PrepareUrl<ContentController>(x => x.PostSave(null));

            var contentService = GetRequiredService<IContentService>();
            var contentTypeService = GetRequiredService<IContentTypeService>();

            var contentType = new ContentTypeBuilder()
                .WithId(0)
                .AddPropertyType()
                .WithAlias("title")
                .WithValueStorageType(ValueStorageType.Integer)
                .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
                .WithName("Title")
                .Done()
                .WithContentVariation(ContentVariation.Nothing)
                .Build();

            contentTypeService.Save(contentType);

            var content = new ContentBuilder()
                .WithId(0)
                .WithName("Invariant")
                .WithContentType(contentType)
                .AddPropertyData()
                .WithKeyValue("title", "Cool invariant title")
                .Done()
                .Build();
            contentService.SaveAndPublish(content);

            var model = new ContentItemSaveBuilder()
                .WithContent(content)
                .Build();

            // HERE we force the test to fail
            model.Variants = model.Variants.Select(x=>
            {
                x.Save = false;
                return x;
            });

            // Act
            var response = await Client.PostAsync(url, new MultipartFormDataContent
            {
                { new StringContent(JsonConvert.SerializeObject(model)), "contentItem" }
            });

            // Assert

            var body = await response.Content.ReadAsStringAsync();

             Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
             Assert.AreEqual(")]}',\n{\"message\":\"No variants flagged for saving\"}", body);
        }

        /// <summary>
        ///     Returns 404 if any of the posted properties dont actually exist
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task PostSave_Validate_Properties_Exist()
        {
            var url = PrepareUrl<ContentController>(x => x.PostSave(null));

            var contentService = GetRequiredService<IContentService>();
            var contentTypeService = GetRequiredService<IContentTypeService>();

            var contentType = new ContentTypeBuilder()
                .WithId(0)
                .AddPropertyType()
                .WithAlias("title")
                .WithValueStorageType(ValueStorageType.Integer)
                .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
                .WithName("Title")
                .Done()
                .WithContentVariation(ContentVariation.Nothing)
                .Build();

            contentTypeService.Save(contentType);

            var content = new ContentBuilder()
                .WithId(0)
                .WithName("Invariant")
                .WithContentType(contentType)
                .AddPropertyData()
                .WithKeyValue("title", "Cool invariant title")
                .Done()
                .Build();
            contentService.SaveAndPublish(content);

            var model = new ContentItemSaveBuilder()
                .WithId(content.Id)
                .WithContentTypeAlias(content.ContentType.Alias)
                .AddVariant()
                    .AddProperty()
                        .WithId(2)
                        .WithAlias("doesntexists")
                        .WithValue("Whatever")
                        .Done()
                    .Done()
                .Build();


            // Act
            var response = await Client.PostAsync(url, new MultipartFormDataContent
            {
                { new StringContent(JsonConvert.SerializeObject(model)), "contentItem" }
            });

            // Assert

            var body = await response.Content.ReadAsStringAsync();

            body = body.TrimStart(AngularJsonMediaTypeFormatter.XsrfPrefix);

             Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Test]
        public async Task PostSave_Simple_Invariant()
        {
            var url = PrepareUrl<ContentController>(x => x.PostSave(null));

            var contentService = GetRequiredService<IContentService>();
            var contentTypeService = GetRequiredService<IContentTypeService>();

            var contentType = new ContentTypeBuilder()
                .WithId(0)
                .AddPropertyType()
                .WithAlias("title")
                .WithValueStorageType(ValueStorageType.Integer)
                .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
                .WithName("Title")
                .Done()
                .WithContentVariation(ContentVariation.Nothing)
                .Build();

            contentTypeService.Save(contentType);

            var content = new ContentBuilder()
                .WithId(0)
                .WithName("Invariant")
                .WithContentType(contentType)
                .AddPropertyData()
                .WithKeyValue("title", "Cool invariant title")
                .Done()
                .Build();
            contentService.SaveAndPublish(content);
            var model = new ContentItemSaveBuilder()
                .WithContent(content)
                .Build();


            // Act
            var response = await Client.PostAsync(url, new MultipartFormDataContent
            {
                { new StringContent(JsonConvert.SerializeObject(model)), "contentItem" }
            });

            // Assert

            var body = await response.Content.ReadAsStringAsync();

            body = body.TrimStart(AngularJsonMediaTypeFormatter.XsrfPrefix);

             Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, body);
             var display = JsonConvert.DeserializeObject<ContentItemDisplay>(body);
             Assert.AreEqual(1, display.Variants.Count());
        }

        [Test]
        public async Task PostSave_Validate_Empty_Name()
        {
            var url = PrepareUrl<ContentController>(x => x.PostSave(null));

            var contentService = GetRequiredService<IContentService>();
            var contentTypeService = GetRequiredService<IContentTypeService>();

            var contentType = new ContentTypeBuilder()
                .WithId(0)
                .AddPropertyType()
                .WithAlias("title")
                .WithValueStorageType(ValueStorageType.Integer)
                .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
                .WithName("Title")
                .Done()
                .WithContentVariation(ContentVariation.Nothing)
                .Build();

            contentTypeService.Save(contentType);

            var content = new ContentBuilder()
                .WithId(0)
                .WithName("Invariant")
                .WithContentType(contentType)
                .AddPropertyData()
                .WithKeyValue("title", "Cool invariant title")
                .Done()
                .Build();
            contentService.SaveAndPublish(content);

            content.Name = null; // Removes the name of one of the variants to force an error
            var model = new ContentItemSaveBuilder()
                .WithContent(content)
                .Build();


            // Act
            var response = await Client.PostAsync(url, new MultipartFormDataContent
            {
                { new StringContent(JsonConvert.SerializeObject(model)), "contentItem" }
            });

            // Assert

            var body = await response.Content.ReadAsStringAsync();

            body = body.TrimStart(AngularJsonMediaTypeFormatter.XsrfPrefix);

             Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
             var display = JsonConvert.DeserializeObject<ContentItemDisplay>(body);
             Assert.AreEqual(1, display.Errors.Count());
             Assert.IsTrue(display.Errors.ContainsKey("Variants[0].Name"));
             //ModelState":{"Variants[0].Name":["Required"]}
        }

        [Test]
        public async Task PostSave_Validate_Variants_Empty_Name()
        {
            var url = PrepareUrl<ContentController>(x => x.PostSave(null));

            var contentService = GetRequiredService<IContentService>();
            var contentTypeService = GetRequiredService<IContentTypeService>();

            var contentType = new ContentTypeBuilder()
                .WithId(0)
                .AddPropertyType()
                .WithAlias("title")
                .WithValueStorageType(ValueStorageType.Integer)
                .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
                .WithName("Title")
                .Done()
                .WithContentVariation(ContentVariation.Culture)
                .Build();

            contentTypeService.Save(contentType);

            var content = new ContentBuilder()
                .WithId(0)
                .WithCultureName("en-US", "English")
                .WithCultureName("da-DK", "Danish")
                .WithContentType(contentType)
                .AddPropertyData()
                .WithKeyValue("title", "Cool invariant title")
                .Done()
                .Build();
            contentService.SaveAndPublish(content);

            content.CultureInfos[0].Name = null; // Removes the name of one of the variants to force an error
            var model = new ContentItemSaveBuilder()
                .WithContent(content)
                .Build();

            // Act
            var response = await Client.PostAsync(url, new MultipartFormDataContent
            {
                { new StringContent(JsonConvert.SerializeObject(model)), "contentItem" }
            });

            // Assert

            var body = await response.Content.ReadAsStringAsync();

            body = body.TrimStart(AngularJsonMediaTypeFormatter.XsrfPrefix);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            var display = JsonConvert.DeserializeObject<ContentItemDisplay>(body);
            Assert.AreEqual(2, display.Errors.Count());
            Assert.IsTrue(display.Errors.ContainsKey("Variants[0].Name"));
            Assert.IsTrue(display.Errors.ContainsKey("_content_variant_en-US_null_"));
        }


        private class TestContentItemSave : ContentItemSave
        {
        }
    }
}
