using System;
using System.Runtime.Serialization;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEngine.ResourceManagement.Exceptions
{
    /// <summary>
    /// Base class for all ResourceManager related exceptions.
    /// </summary>
    public class ResourceManagerException : Exception
    {
        /// <summary>
        /// Construct a new ResourceManagerException.
        /// </summary>
        public ResourceManagerException() { }
        /// <summary>
        /// Construct a new ResourceManagerException.
        /// </summary>
        /// <param name="message">Message to describe the exception.</param>
        public ResourceManagerException(string message) : base(message) { }
        /// <summary>
        /// Construct a new ResourceManagerException.
        /// </summary>
        /// <param name="message">Message to describe the exception.</param>
        /// <param name="innerException">Inner exception that caused this exception.</param>
        public ResourceManagerException(string message, Exception innerException) : base(message, innerException) { }
        /// <summary>
        /// Construct a new ResourceManagerException.
        /// </summary>
        /// <param name="message">Message to describe the exception.</param>
        /// <param name="context">Context related to the exception.</param>
        protected ResourceManagerException(SerializationInfo message, StreamingContext context) : base(message, context) { }
    }
    /// <summary>
    /// Exception returned when the IResourceProvider is not found for a location.
    /// </summary>
    public class UnknownResourceProviderException : ResourceManagerException
    {
        /// <summary>
        /// The location that contains the provider id that was not found.
        /// </summary>
        public IResourceLocation Location { get; private set; }
        /// <summary>
        /// Construct a new UnknownResourceProviderException
        /// </summary>
        /// <param name="location">The location that caused the exception to be created.</param>
        public UnknownResourceProviderException(IResourceLocation location)
        {
            Location = location;
        }
        /// <summary>
        ///  Construct a new UnknownResourceProviderException
        /// </summary>
        public UnknownResourceProviderException() { }
        /// <summary>
        /// Construct a new UnknownResourceProviderException
        /// </summary>
        /// <param name="message">Message to describe the exception.</param>
        public UnknownResourceProviderException(string message) : base(message) { }
        /// <summary>
        /// Construct a new UnknownResourceProviderException
        /// </summary>
        /// <param name="message">Message to describe the exception.</param>
        /// <param name="innerException">Inner exception that caused this exception.</param>
        public UnknownResourceProviderException(string message, Exception innerException) : base(message, innerException) { }
        /// <summary>
        /// Construct a new UnknownResourceProviderException
        /// </summary>
        /// <param name="message">Message to describe the exception.</param>
        /// <param name="context">Context related to the exception.</param>
        protected UnknownResourceProviderException(SerializationInfo message, StreamingContext context) : base(message, context) { }

        /// <summary>
        /// Returns a string describing  this exception
        /// </summary>
        public override string Message
        {
            get
            {
                return base.Message + ", ProviderId=" + Location.ProviderId + ", Location=" + Location;
            }
        }
        /// <summary>
        /// Returns string representation of exception.
        /// </summary>
        /// <returns>String representation of exception.</returns>
        public override string ToString()
        {
            return Message;
        }

    }
}