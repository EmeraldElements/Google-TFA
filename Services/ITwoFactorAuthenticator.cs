using Orchard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EmeraldElements.TwoFactorAuthentication.Services
{
    public interface ITwoFactorAuthenticator : IDependency
    {
        /// <summary>
        /// Generates a key to be used in pin generation
        /// </summary>        
        byte[] GenerateKey();

        ///// <summary>
        ///// Generates a temporary access pin
        ///// </summary>
        ///// <param name="key">The key to be used in generating the pin.</param>
        string GeneratePin(byte[] key);

        /// <summary>
        /// Generates an image representation of the key
        /// </summary>
        /// <param name="identifier">The description to be displayed for this account.</param>
        /// <param name="key">The key to be used in generating the image.</param>
        /// <param name="width">The image's width.</param>
        /// <param name="height">The image's height.</param>
        byte[] GenerateProvisioningImage(string identifier, byte[] key, int width, int height);       

        /// <summary>
        /// Tests the validity of a temporary access pin
        /// </summary>
        /// <param name="key">The key to be used in testing the pin.</param>
        /// <param name="pin">The pin to test.</param>
        /// <param name="checkAdjacentIntervals">Number of intervals to consider valid.</param>
        bool IsValid(byte[] key, string pin, int checkAdjacentIntervals);

        /// <summary>
        /// Encodes the key as a string
        /// </summary>
        /// <param name="keyData">The data to encode as a string.</param>
        string EncodeKey(byte[] keyData);
    }
}