using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace MultiPartFormDataNet.Core.Extensions
{
    public static class MultiPartFormDataExtensions
    {
        private static readonly FormOptions DefaultFormOptions = new FormOptions();

        /// <summary>
        ///     Read all properties from object and add values to <paramref name="content" />.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="obj">Object to read properties</param>
        /// <example>
        ///     <code>
        /// var content = new MultipartFormDataContent();
        /// var data = new {
        ///     PropertyValue = "Value",
        ///     PropertyIntValue = 1,
        ///     PropertyArrayValue = new [] { 1, 2, 3 } 
        /// };
        /// content.AddJsonObject(data);
        /// </code>
        /// </example>
        public static void AddJsonObject(this MultipartFormDataContent content, object obj)
        {
            var properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).ToList();
            properties.ForEach(property =>
            {
                if (property.PropertyType.IsArray ||
                    (property.PropertyType.GetInterface(nameof(IEnumerable)) != null &&
                     property.PropertyType != typeof(string)))
                {
                    var list = (IEnumerable<object>) property.GetValue(obj);
                    list.ToList().ForEach(value =>
                        content.Add(new StringContent(value.ToString()), $"{property.Name}[]"));
                }
                else
                { 
                    content.Add(new StringContent(property.GetValue(obj).ToString()), property.Name);
                }
            });
        }

        /// <summary>
        ///     Copy file stream to <paramref name="targetStream" /> and creates and returns an instance of
        ///     <typeparamref name="TDto" />
        ///     with properties fulfilled.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="targetStream"></param>
        /// <typeparam name="TDto"></typeparam>
        /// <returns>Instance of <typeparamref name="TDto" /></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task<TDto> ReadMultiPartFormData<TDto>(this ControllerBase controller, Stream targetStream)
            where TDto : class, new()
        {
            if (IsMultipartContentType(controller.Request.ContentType) == false)
                throw new Exception($"Expected a multipart request, but got {controller.Request.ContentType}");

            var boundary = MediaTypeHeaderValue.Parse(controller.Request.ContentType)
                .GetBoundary(DefaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, controller.Request.Body);

            var formAccumulator = new KeyValueAccumulator();

            var section = await reader.ReadNextSectionAsync();
            do
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                {
                    if (contentDisposition.HasFile())
                    {
                        await section.Body.CopyToAsync(targetStream);
                    }
                    else if (contentDisposition.HasFormData())
                    {
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                        var encoding = GetEncoding(section);
                        using (var streamReader = new StreamReader(section.Body, encoding))
                        {
                            var value = await streamReader.ReadToEndAsync();
                            if (string.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                                value = string.Empty;

                            formAccumulator.Append(key.Value.Replace("[]", string.Empty), value);

                            if (formAccumulator.ValueCount > DefaultFormOptions.ValueCountLimit)
                                throw new InvalidDataException(
                                    $"Form key count limit {DefaultFormOptions.ValueCountLimit} exceeded.");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Content type invalid.");
                    }
                }

                section = await reader.ReadNextSectionAsync();
            } while (section != null);

            return await GetModel<TDto>(controller, formAccumulator);
        }

        private static async Task<TDto> GetModel<TDto>(ControllerBase controller, KeyValueAccumulator formAccumulator)
            where TDto : class, new()
        {
            var valueProvider = new FormValueProvider(
                BindingSource.Form,
                new FormCollection(formAccumulator.GetResults()),
                CultureInfo.CurrentCulture);

            var model = Activator.CreateInstance<TDto>();
            if (await controller.TryUpdateModelAsync(model, string.Empty, valueProvider) == false)
                throw new InvalidOperationException($"Could not update model {model.GetType().Name}.");

            return model;
        }

        private static Encoding GetEncoding(MultipartSection section)
        {
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out var mediaType);
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding)) return Encoding.UTF8;

            return mediaType.Encoding;
        }

        private static string GetBoundary(this MediaTypeHeaderValue contentType, int lengthLimit)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
            if (string.IsNullOrWhiteSpace(boundary)) throw new InvalidDataException("Missing content-type boundary.");

            if (boundary.Length > lengthLimit)
                throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded.");

            return boundary;
        }

        private static bool IsMultipartContentType(string contentType)
        {
            return string.IsNullOrWhiteSpace(contentType) == false
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasFormData(this ContentDispositionHeaderValue contentDisposition)
        {
            return contentDisposition.DispositionType.Equals("form-data")
                   && string.IsNullOrWhiteSpace(contentDisposition.FileName.Value)
                   && string.IsNullOrWhiteSpace(contentDisposition.FileNameStar.Value);
        }

        private static bool HasFile(this ContentDispositionHeaderValue contentDisposition)
        {
            return contentDisposition.DispositionType.Equals("form-data")
                   && (string.IsNullOrWhiteSpace(contentDisposition.FileName.Value) == false ||
                       string.IsNullOrWhiteSpace(contentDisposition.FileNameStar.Value) == false);
        }
    }
}