
** `[Open Source] WebVella.Database - A lightweight Postgres library with nested transactions, advisory locks, and RLS support`

---

## Post Body

I’ve been working on a data access library for PostgreSQL that I believe fills several gaps left by existing solutions. After using this concept in production for over 10 years, I decided to release it as an open-source library.

# What is it?

**WebVella.Database** is a lightweight, high-performance Postgres data access library built on Dapper. It focuses on things that are often painful to implement correctly:

* ✅ **Nested transactions** that actually work (proper savepoint handling)
* ✅ **Advisory locks** with simple scopes (great for distributed locking)
* ✅ **Row Level Security (RLS)** with automatic session context injection
* ✅ **Database migrations** with version control and rollback
* ✅ **Entity caching** that's RLS-aware (different tenants = different cache keys)

# More information about how to use it, nuget package and source code at:

* 📖 **Docs**: [Full documentation](https://github.com/WebVella/WebVella.Database/blob/main/docs/index.md)
* 📦 **NuGet**: [WebVella.Database](https://www.nuget.org/packages/WebVella.Database/)
* 📂 **GitHub**: [github.com/WebVella/WebVella.Database](https://github.com/WebVella/WebVella.Database)

# Looking for feedback!

I'd love to hear:

* What features would make this more useful for you?
* Use cases I might not have considered?

The library is MIT licensed. PRs and issues welcome!