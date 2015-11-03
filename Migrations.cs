using System;
using System.Collections.Generic;
using System.Data;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;

namespace EmeraldElements.TwoFactorAuthentication {
    public class Migrations : DataMigrationImpl {

        public int Create() {
			// Creating table TwoFactorAuthenticationPartRecord
			SchemaBuilder.CreateTable("TwoFactorAuthenticationPartRecord", table => table
				.ContentPartRecord()
				.Column("EnableTFA", DbType.Boolean)
				.Column("SecretKey", DbType.Binary)
				.Column("HasVerifiedKey", DbType.Boolean)
				.Column("CanLoginWithoutTFA", DbType.Boolean)
				.Column("EnableBackupSMS", DbType.Boolean)
				.Column("BackupSMSNum", DbType.String)
			);

            ContentDefinitionManager.AlterTypeDefinition("User",
                cfg => cfg
                    .WithPart("TwoFactorAuthenticationPart")
                );

            return 1;
        }
    }
}