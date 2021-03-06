﻿using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using autoapi.Providers;
using Microsoft.AspNet.Identity.EntityFramework;

namespace autoapi
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoApiAttribute : Attribute
    {
        public bool AnyUserCanRead { get; set; }

        public bool AnyUserCanAdd { get; set; }

        public bool OwnerCanAdd { get; set; }

        public bool OwnerCanDelete { get; set; }

        public bool OwnerCanEdit { get; set; }

        public bool OwnerHasFullControll
        {
            get { return OwnerCanAdd && OwnerCanDelete && OwnerCanEdit; }
            set { OwnerCanAdd = OwnerCanDelete = OwnerCanEdit = value; }
        }

        public AutoApiAttribute(bool ownerHasFullControll = false)
        {
            OwnerHasFullControll = ownerHasFullControll;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class AutoPropertyAttribute : Attribute
    {
        public bool OwnerCanSet { get; set; }
        public string PropertyName { get; set; }
        public bool Nest { get; set; }
    }

    public abstract class AutoApiUser : IdentityUser<Guid, AutoApiUserLogin, AutoApiUserRole, AutoApiUserClaim>, IIdentifiable
    {
        private static readonly ConcurrentDictionary<Guid, bool> AdminCache 
            = new ConcurrentDictionary<Guid, bool>();

        public const string
            AdminRole = "Admin",
            APISelf = "api/self";

        [Required]
        [AutoProperty]
        public override string UserName { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        [NotMapped]
        [AutoProperty(PropertyName = Item.TypeIdentityShortName)]
        public string TypeIdentity => Item.GetTypeIdentity(GetType());

        [AutoProperty]
        public override Guid Id
        {
            get { return base.Id; }
            set { base.Id = value; }
        }

        [NotMapped]
        [AutoProperty]
        public bool IsAdmin
        {
            get
            {
                return AdminCache.GetOrAdd(Id, id => Roles.Any(r => r.AutoApiRole.Name == AdminRole));
            }
        }
    }

    public abstract class Item : IIdentifiable
    {
        public const string
            IdentityShortName = "id",
            TypeIdentityShortName = "type";

        [Index]
        [Required]
        public bool IsDeleted { get; set; }

        [AutoProperty]
        public DateTime CreatedOn
        {
            get
            {
                var bytes = Id.ToByteArray().Skip(8).Reverse().ToArray();
                var ticks = BitConverter.ToInt64(bytes, 0);
                return new DateTime(ticks);
            }
        }

        #region Identity

        [Key]
        [AutoProperty(PropertyName = IdentityShortName)]
        public Guid Id { get; set; }

        [NotMapped]
        [AutoProperty(PropertyName = TypeIdentityShortName)]
        public virtual string TypeIdentity => GetTypeIdentity(GetType());

        public static string GetTypeIdentity(Type type)
        {
            var tt = type;
            while (type.FullName.Contains("DynamicProxies"))
            {
                type = type.BaseType;
                if (type == null)
                    throw new Exception($"Unable to get base type for {tt}");
            }

            return type.FullName;
        }

        [AutoProperty(OwnerCanSet = true)]
        public virtual string Name { get; set; }

        protected Item()
        {
            Id = CreateTimeGuid();
        }

        protected Guid CreateTimeGuid()
        {
            var tail = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            var buffer = new byte[8];
            ThreadLocalRandomProvider.Instance.NextBytes(buffer);
            return new Guid(buffer.Concat(tail.Reverse()).ToArray());
        }

        protected Guid CreateRandomGuid()
        {
            var buffer = new byte[16];
            ThreadLocalRandomProvider.Instance.NextBytes(buffer);
            return new Guid(buffer);
        }

        #endregion
    }

    public abstract class Item<TSource> : Item
    {
        [Required]
        [AutoProperty]
        [Index]
        public Guid SourceId { get; set; }

        [ForeignKey(nameof(SourceId))]
        public virtual TSource Source { get; set; }
    }

    public abstract class Edge<TSource, TDestination> : Item<TSource>
    {
        [Required]
        [AutoProperty]
        public Guid DestinationId { get; set; }

        [ForeignKey(nameof(DestinationId))]
        public virtual TDestination Destination { get; set; }
    }
}
