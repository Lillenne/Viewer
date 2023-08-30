﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Viewer.Server.Services;

#nullable disable

namespace Viewer.Server.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class UserContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.10")
                .HasAnnotation("Proxies:ChangeTracking", false)
                .HasAnnotation("Proxies:CheckEquality", false)
                .HasAnnotation("Proxies:LazyLoading", true)
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Viewer.Server.Models.Album", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid?>("UserGroupId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserGroupId");

                    b.HasIndex("UserId");

                    b.ToTable("Album");
                });

            modelBuilder.Entity("Viewer.Server.Models.GroupMember", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<int>("Role")
                        .HasColumnType("integer");

                    b.Property<Guid?>("UserGroupId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserGroupId");

                    b.ToTable("GroupMember");
                });

            modelBuilder.Entity("Viewer.Server.Models.Upload", b =>
                {
                    b.Property<Guid>("UploadId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("AlbumId")
                        .HasColumnType("uuid");

                    b.Property<string>("DirectoryPrefix")
                        .HasColumnType("text");

                    b.Property<string>("OriginalFileName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("OwnerId")
                        .HasColumnType("uuid");

                    b.Property<int>("Visibility")
                        .HasColumnType("integer");

                    b.HasKey("UploadId");

                    b.HasIndex("AlbumId");

                    b.HasIndex("OwnerId");

                    b.ToTable("Uploads");
                });

            modelBuilder.Entity("Viewer.Server.Models.User", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("FirstName")
                        .HasColumnType("text");

                    b.Property<string>("LastName")
                        .HasColumnType("text");

                    b.Property<byte[]>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.Property<byte[]>("PasswordSalt")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uuid");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Viewer.Server.Models.UserGroup", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserGroups");
                });

            modelBuilder.Entity("Viewer.Server.Models.Album", b =>
                {
                    b.HasOne("Viewer.Server.Models.UserGroup", null)
                        .WithMany("Albums")
                        .HasForeignKey("UserGroupId");

                    b.HasOne("Viewer.Server.Models.User", null)
                        .WithMany("Albums")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("Viewer.Server.Models.GroupMember", b =>
                {
                    b.HasOne("Viewer.Server.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Viewer.Server.Models.UserGroup", null)
                        .WithMany("Members")
                        .HasForeignKey("UserGroupId");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Viewer.Server.Models.Upload", b =>
                {
                    b.HasOne("Viewer.Server.Models.Album", null)
                        .WithMany("Uploads")
                        .HasForeignKey("AlbumId");

                    b.HasOne("Viewer.Server.Models.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("Viewer.Server.Models.User", b =>
                {
                    b.HasOne("Viewer.Server.Models.User", null)
                        .WithMany("Friends")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("Viewer.Server.Models.UserGroup", b =>
                {
                    b.HasOne("Viewer.Server.Models.User", null)
                        .WithMany("Groups")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("Viewer.Server.Models.Album", b =>
                {
                    b.Navigation("Uploads");
                });

            modelBuilder.Entity("Viewer.Server.Models.User", b =>
                {
                    b.Navigation("Albums");

                    b.Navigation("Friends");

                    b.Navigation("Groups");
                });

            modelBuilder.Entity("Viewer.Server.Models.UserGroup", b =>
                {
                    b.Navigation("Albums");

                    b.Navigation("Members");
                });
#pragma warning restore 612, 618
        }
    }
}
