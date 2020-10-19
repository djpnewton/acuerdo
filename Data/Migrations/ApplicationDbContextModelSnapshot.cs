﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using viafront3.Data;

namespace viafront3.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .HasColumnType("varchar(256)")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedName")
                        .HasColumnType("varchar(256)")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasName("RoleNameIndex");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ClaimType")
                        .HasColumnType("longtext");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("longtext");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ClaimType")
                        .HasColumnType("longtext");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("longtext");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("longtext");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("RoleId")
                        .HasColumnType("varchar(255)");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Name")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Value")
                        .HasColumnType("longtext");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("viafront3.Data.TripwireEvent", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("Date")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("RemoteIpAddress")
                        .HasColumnType("longtext");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("TripwireEvents");
                });

            modelBuilder.Entity("viafront3.Models.AccountCreationRequest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ApplicationUserId")
                        .HasColumnType("longtext");

                    b.Property<bool>("Completed")
                        .HasColumnType("tinyint(1)");

                    b.Property<long>("Date")
                        .HasColumnType("bigint");

                    b.Property<string>("RequestedDeviceName")
                        .HasColumnType("longtext");

                    b.Property<string>("RequestedEmail")
                        .HasColumnType("longtext");

                    b.Property<string>("Secret")
                        .HasColumnType("longtext");

                    b.Property<string>("Token")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("Token")
                        .IsUnique();

                    b.ToTable("AccountCreationRequests");
                });

            modelBuilder.Entity("viafront3.Models.ApiKey", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("AccountCreationRequestId")
                        .HasColumnType("int");

                    b.Property<int>("ApiKeyCreationRequestId")
                        .HasColumnType("int");

                    b.Property<string>("ApplicationUserId")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Key")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Name")
                        .HasColumnType("longtext");

                    b.Property<long>("Nonce")
                        .HasColumnType("bigint");

                    b.Property<string>("Secret")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("ApplicationUserId");

                    b.HasIndex("Key")
                        .IsUnique();

                    b.ToTable("ApiKeys");
                });

            modelBuilder.Entity("viafront3.Models.ApiKeyCreationRequest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ApplicationUserId")
                        .HasColumnType("longtext");

                    b.Property<bool>("Completed")
                        .HasColumnType("tinyint(1)");

                    b.Property<long>("Date")
                        .HasColumnType("bigint");

                    b.Property<string>("RequestedDeviceName")
                        .HasColumnType("longtext");

                    b.Property<string>("Secret")
                        .HasColumnType("longtext");

                    b.Property<string>("Token")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("Token")
                        .IsUnique();

                    b.ToTable("ApiKeyCreationRequests");
                });

            modelBuilder.Entity("viafront3.Models.ApplicationUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(255)");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("int");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("longtext");

                    b.Property<string>("Email")
                        .HasColumnType("varchar(256)")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("NormalizedEmail")
                        .HasColumnType("varchar(256)")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasColumnType("varchar(256)")
                        .HasMaxLength(256);

                    b.Property<string>("PasswordHash")
                        .HasColumnType("longtext");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("longtext");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("longtext");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("UserName")
                        .HasColumnType("varchar(256)")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("viafront3.Models.AuthenticationTicket", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTimeOffset?>("Expires")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTimeOffset?>("LastActivity")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("OperatingSystem")
                        .HasColumnType("longtext");

                    b.Property<string>("RemoteIpAddress")
                        .HasColumnType("longtext");

                    b.Property<string>("UserAgentFamily")
                        .HasColumnType("longtext");

                    b.Property<string>("UserAgentVersion")
                        .HasColumnType("longtext");

                    b.Property<string>("UserId")
                        .HasColumnType("varchar(255)");

                    b.Property<byte[]>("Value")
                        .HasColumnType("longblob");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AuthenticationTickets");
                });

            modelBuilder.Entity("viafront3.Models.BrokerOrder", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<decimal>("AmountReceive")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("AmountSend")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("ApplicationUserId")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("AssetReceive")
                        .HasColumnType("longtext");

                    b.Property<string>("AssetSend")
                        .HasColumnType("longtext");

                    b.Property<long>("Date")
                        .HasColumnType("bigint");

                    b.Property<long>("Expiry")
                        .HasColumnType("bigint");

                    b.Property<decimal>("Fee")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("InvoiceId")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Market")
                        .HasColumnType("longtext");

                    b.Property<string>("PaymentAddress")
                        .HasColumnType("longtext");

                    b.Property<string>("PaymentUrl")
                        .HasColumnType("longtext");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Recipient")
                        .HasColumnType("longtext");

                    b.Property<int>("Side")
                        .HasColumnType("int");

                    b.Property<string>("Status")
                        .HasColumnType("longtext");

                    b.Property<string>("Token")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("TxIdPayment")
                        .HasColumnType("longtext");

                    b.Property<string>("TxIdRecipient")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("ApplicationUserId");

                    b.HasIndex("InvoiceId")
                        .IsUnique();

                    b.HasIndex("Token")
                        .IsUnique();

                    b.ToTable("BrokerOrders");
                });

            modelBuilder.Entity("viafront3.Models.BrokerOrderChainWithdrawal", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("BrokerOrderId")
                        .HasColumnType("int");

                    b.Property<string>("SpendCode")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("BrokerOrderId")
                        .IsUnique();

                    b.HasIndex("SpendCode")
                        .IsUnique();

                    b.ToTable("BrokerOrderChainWithdrawals");
                });

            modelBuilder.Entity("viafront3.Models.BrokerOrderCustomRecipientParams", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("BrokerOrderId")
                        .HasColumnType("int");

                    b.Property<string>("Code")
                        .HasColumnType("longtext");

                    b.Property<string>("Particulars")
                        .HasColumnType("longtext");

                    b.Property<string>("Reference")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("BrokerOrderId")
                        .IsUnique();

                    b.ToTable("BrokerOrderCustomRecipientParams");
                });

            modelBuilder.Entity("viafront3.Models.BrokerOrderFiatWithdrawal", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("BrokerOrderId")
                        .HasColumnType("int");

                    b.Property<string>("DepositCode")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("BrokerOrderId")
                        .IsUnique();

                    b.HasIndex("DepositCode")
                        .IsUnique();

                    b.ToTable("BrokerOrderFiatWithdrawals");
                });

            modelBuilder.Entity("viafront3.Models.Exchange", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ApplicationUserId")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("ApplicationUserId")
                        .IsUnique();

                    b.ToTable("Exchange");
                });

            modelBuilder.Entity("viafront3.Models.Kyc", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ApplicationUserId")
                        .HasColumnType("varchar(255)");

                    b.Property<int>("Level")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ApplicationUserId")
                        .IsUnique();

                    b.ToTable("Kycs");
                });

            modelBuilder.Entity("viafront3.Models.KycRequest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ApplicationUserId")
                        .HasColumnType("longtext");

                    b.Property<long>("Date")
                        .HasColumnType("bigint");

                    b.Property<string>("Token")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("Token")
                        .IsUnique();

                    b.ToTable("KycRequests");
                });

            modelBuilder.Entity("viafront3.Models.OAuthToken", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("AccessToken")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("ApplicationUserId")
                        .HasColumnType("varchar(255)");

                    b.Property<long>("Date")
                        .HasColumnType("bigint");

                    b.Property<long>("ExpiresAt")
                        .HasColumnType("bigint");

                    b.Property<long>("ExpiresIn")
                        .HasColumnType("bigint");

                    b.Property<string>("Scope")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("AccessToken")
                        .IsUnique();

                    b.HasIndex("ApplicationUserId");

                    b.ToTable("OAuthTokens");
                });

            modelBuilder.Entity("viafront3.Models.Withdrawal", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Amount")
                        .HasColumnType("longtext");

                    b.Property<string>("ApplicationUserId")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Asset")
                        .HasColumnType("longtext");

                    b.Property<long>("Date")
                        .HasColumnType("bigint");

                    b.Property<string>("WithdrawalAssetEquivalent")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("ApplicationUserId");

                    b.ToTable("Withdrawals");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("viafront3.Models.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("viafront3.Models.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("viafront3.Models.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("viafront3.Models.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("viafront3.Models.ApiKey", b =>
                {
                    b.HasOne("viafront3.Models.ApplicationUser", null)
                        .WithMany("ApiKeys")
                        .HasForeignKey("ApplicationUserId");
                });

            modelBuilder.Entity("viafront3.Models.AuthenticationTicket", b =>
                {
                    b.HasOne("viafront3.Models.ApplicationUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("viafront3.Models.BrokerOrder", b =>
                {
                    b.HasOne("viafront3.Models.ApplicationUser", "User")
                        .WithMany()
                        .HasForeignKey("ApplicationUserId");
                });

            modelBuilder.Entity("viafront3.Models.BrokerOrderCustomRecipientParams", b =>
                {
                    b.HasOne("viafront3.Models.BrokerOrder", "BrokerOrder")
                        .WithMany()
                        .HasForeignKey("BrokerOrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("viafront3.Models.Exchange", b =>
                {
                    b.HasOne("viafront3.Models.ApplicationUser", null)
                        .WithOne("Exchange")
                        .HasForeignKey("viafront3.Models.Exchange", "ApplicationUserId");
                });

            modelBuilder.Entity("viafront3.Models.Kyc", b =>
                {
                    b.HasOne("viafront3.Models.ApplicationUser", null)
                        .WithOne("Kyc")
                        .HasForeignKey("viafront3.Models.Kyc", "ApplicationUserId");
                });

            modelBuilder.Entity("viafront3.Models.OAuthToken", b =>
                {
                    b.HasOne("viafront3.Models.ApplicationUser", "User")
                        .WithMany()
                        .HasForeignKey("ApplicationUserId");
                });

            modelBuilder.Entity("viafront3.Models.Withdrawal", b =>
                {
                    b.HasOne("viafront3.Models.ApplicationUser", null)
                        .WithMany("Withdrawals")
                        .HasForeignKey("ApplicationUserId");
                });
#pragma warning restore 612, 618
        }
    }
}
