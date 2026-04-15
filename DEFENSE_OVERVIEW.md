# JAS-MINE System - Comprehensive Defense Overview
**Joint Administrative System for Managing Information and Network Efficiency**

---

## 📋 TABLE OF CONTENTS
1. [System Overview](#1-system-overview)
2. [Technology Stack & Architecture](#2-technology-stack--architecture)
3. [User Roles & Permissions](#3-user-roles--permissions)
4. [All Modules & Features](#4-all-modules--features)
5. [Transaction Flows](#5-transaction-flows)
6. [Database Schema](#6-database-schema)
7. [Security Features](#7-security-features)
8. [External Integrations](#8-external-integrations)
9. [Key Controllers & Services](#9-key-controllers--services)
10. [API Endpoints](#10-api-endpoints)
11. [Subscription Plans](#11-subscription-plans)
12. [Defense Q&A Preparation](#12-defense-qa-preparation)

---

## 1. SYSTEM OVERVIEW

### What is JAS-MINE?
**JAS-MINE** (Joint Administrative System for Managing Information and Network Efficiency) is a **multi-tenant SaaS (Software-as-a-Service) Knowledge Management System** designed specifically for **Philippine Barangays** (the smallest administrative division in the Philippines).

### Problems It Solves

| Problem | Solution |
|---------|----------|
| **Fragmented Knowledge Management** | Centralized digital repository for all documents, policies, and practices |
| **Knowledge Loss** | When staff changes, institutional knowledge is preserved in the system |
| **No Standardized Documentation** | Structured templates for policies, lessons learned, best practices |
| **Inefficient Communication** | Real-time announcements with priority levels and notifications |
| **Manual Subscription Management** | Automated subscription tracking with payment integration |
| **Lack of Audit Trails** | Comprehensive logging of all user actions |
| **Knowledge Silos** | Platform for sharing best practices across barangays |

### Target Users
- **Super Admin**: System administrator managing all barangays
- **Barangay Officials**: Captains, council members, secretaries
- **Barangay Staff**: Administrative personnel handling daily operations

---

## 2. TECHNOLOGY STACK & ARCHITECTURE

### Technology Stack

| Layer | Technology | Purpose |
|-------|------------|---------|
| **Backend Framework** | ASP.NET Core 8.0 | MVC web application framework |
| **ORM** | Entity Framework Core | Database abstraction |
| **Database** | Microsoft SQL Server | Data persistence |
| **Authentication** | ASP.NET Core Identity | User management & authentication |
| **Real-time** | SignalR | WebSocket-based notifications |
| **Payment Gateway** | PayMongo API | Philippine payment processing |
| **Logging** | Serilog | Structured logging |
| **Frontend** | Razor Views + Bootstrap 5 | Server-side rendering with responsive UI |

### Multi-Tenant Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    SUPER ADMIN (System-wide)                 │
│         Can see all barangays, manage subscriptions          │
└─────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐     ┌───────────────┐     ┌───────────────┐
│  Barangay A   │     │  Barangay B   │     │  Barangay C   │
│  (Tenant 1)   │     │  (Tenant 2)   │     │  (Tenant 3)   │
│               │     │               │     │               │
│  ✓ Own Users  │     │  ✓ Own Users  │     │  ✓ Own Users  │
│  ✓ Own Data   │     │  ✓ Own Data   │     │  ✓ Own Data   │
│  ✓ Isolated   │     │  ✓ Isolated   │     │  ✓ Isolated   │
└───────────────┘     └───────────────┘     └───────────────┘
```

### How Multi-Tenancy Works

1. **Session-Based Tenant Context**: When a user logs in, their `BarangayId` is stored in the session
2. **ITenantService**: A service that provides the current tenant context throughout the application
3. **FilterByTenant()**: A LINQ extension method that automatically filters all queries by `BarangayId`
4. **Data Isolation**: Each barangay can ONLY see and access their own data

```csharp
// Example: How tenant filtering works
var documents = _context.KnowledgeRepository
    .FilterByTenant(_tenantService)  // Automatically adds WHERE BarangayId = {current}
    .Where(d => d.IsActive)
    .ToList();
```

---

## 3. USER ROLES & PERMISSIONS

### Role Hierarchy

```
┌─────────────────┐
│   SUPER_ADMIN   │  ← System-wide access (JAS-MINE administrator)
└────────┬────────┘
         │
┌────────▼────────┐
│  BARANGAY_ADMIN │  ← Barangay Captain / IT Admin
└────────┬────────┘
         │
┌────────▼──────────┐
│ BARANGAY_SECRETARY│  ← Barangay Secretary
└────────┬──────────┘
         │
┌────────▼────────┐
│  BARANGAY_STAFF │  ← Regular staff members
└────────┬────────┘
         │
┌────────▼────────┐
│ COUNCIL_MEMBER  │  ← View-only access
└─────────────────┘
```

### Permission Matrix

| Permission | Super Admin | Barangay Admin | Secretary | Staff | Council Member |
|------------|:-----------:|:--------------:|:---------:|:-----:|:--------------:|
| View all barangays | ✅ | ❌ | ❌ | ❌ | ❌ |
| Manage subscriptions | ✅ | ❌ | ❌ | ❌ | ❌ |
| Manage users | ✅ | ✅ | ❌ | ❌ | ❌ |
| Approve content | ✅ | ✅ | ❌ | ❌ | ❌ |
| Create announcements | ✅ | ✅ | ✅ | ❌ | ❌ |
| Create documents | ✅ | ✅ | ✅ | ✅ | ❌ |
| Create policies | ✅ | ✅ | ✅ | ✅ | ❌ |
| Create lessons learned | ✅ | ✅ | ✅ | ✅ | ❌ |
| Create best practices | ✅ | ✅ | ✅ | ✅ | ❌ |
| Participate in discussions | ✅ | ✅ | ✅ | ✅ | ❌ |
| View content | ✅ | ✅ | ✅ | ✅ | ✅ |

### Permission Enforcement Attributes

```csharp
[DenyViewOnly]           // Blocks council_member from modify actions
[RequireRoles("admin")]  // Restricts to specific roles
[RequireActiveSubscription] // Blocks if subscription expired
[Authorize(Roles = "super_admin,barangay_admin")] // Role-based authorization
```

---

## 4. ALL MODULES & FEATURES

### A. 📚 Knowledge Repository Module
**Purpose**: Central document storage and management

| Feature | Description |
|---------|-------------|
| Document Upload | PDF, DOC, XLS, PPT, Images (max 10MB) |
| Categories | Ordinance, Resolution, Report, Certification, Form, Other |
| Tagging | Custom tags for organization |
| Approval Workflow | Pending → Approved/Rejected |
| Version Tracking | Track document revisions |
| View/Download Count | Usage analytics |
| Archive/Restore | Soft delete with recovery |

### B. 📜 Policy Management Module
**Purpose**: Create and manage barangay policies

| Feature | Description |
|---------|-------------|
| Policy Creation | Rich text editor for content |
| Effective Dates | Start and expiry dates |
| Status Workflow | Draft → Pending → Approved |
| File Attachments | Supporting documents |
| Archive/Restore | Policy lifecycle management |

### C. 💡 Lessons Learned Module
**Purpose**: Document organizational lessons using PARC format

| Feature | Description |
|---------|-------------|
| Problem | What was the issue? |
| Action Taken | What was done? |
| Result | What happened? |
| Recommendation | What should others do? |
| Project Type | Categorization |
| Likes | Engagement tracking |

### D. ⭐ Best Practices Module
**Purpose**: Share successful practices across barangays

| Feature | Description |
|---------|-------------|
| Practice Documentation | Purpose, Steps, Resources |
| Rating System | 1-5 star ratings |
| Implementation Tracking | Track adoption |
| Featured Practices | Highlight top practices |

### E. 💬 Knowledge Discussions (Forum)
**Purpose**: Discussion threads and knowledge sharing

| Feature | Description |
|---------|-------------|
| Discussion Threads | Create topics |
| Comments | Reply to discussions |
| Likes | Upvote helpful content |
| Categories | Organize discussions |
| Quick Post | Fast content creation |
| Real-time Notifications | Instant reply alerts |

### F. 📢 Announcements Module
**Purpose**: Official communications and news

| Feature | Description |
|---------|-------------|
| Priority Levels | Low, Medium, High |
| Status | Draft, Published, Archived |
| Pin Important | Sticky announcements |
| Expiry Date | Auto-archive after date |
| View Count | Track readership |

### G. 💳 Subscription Management (Super Admin)
**Purpose**: Manage barangay subscriptions

| Feature | Description |
|---------|-------------|
| Plan Creation | Basic, Professional, Enterprise |
| Plan Features | User limits, feature toggles |
| Assign/Revoke | Manage subscriptions |
| Status Tracking | Active/Pending/Expired/Cancelled |
| Auto-Expiry | Background service for expiration |

### H. 💰 Payment Management
**Purpose**: Track and process subscription payments

| Feature | Description |
|---------|-------------|
| Manual Payments | Record cash/check payments |
| Online Payments | PayMongo integration |
| Invoice Generation | Automated invoicing |
| Payment Proof | Upload verification documents |
| Approval Workflow | Verify before activation |
| Payment Methods | Cash, Bank, GCash, Maya, Check |

### I. 👥 User Management
**Purpose**: Manage system users

| Feature | Description |
|---------|-------------|
| Create Users | Add new users to barangay |
| Role Assignment | Assign appropriate roles |
| Barangay Assignment | Link to tenant |
| Profile Management | Personal info, photo |
| Password Reset | Self-service and admin reset |
| Deactivate Users | Soft delete users |

### J. 🏘️ Barangay Management (Super Admin)
**Purpose**: Register and manage barangays

| Feature | Description |
|---------|-------------|
| Registration | Add new barangays |
| Location Details | Municipality, Province, Region |
| Contact Info | Address, phone, email |
| Archive | Inactive barangay tracking |

### K. 📊 Audit Logging Module
**Purpose**: Track all user activities

| Feature | Description |
|---------|-------------|
| Action Tracking | Create, Update, Delete, Approve, etc. |
| Module Filtering | Filter by system module |
| IP Logging | Track user location |
| Session Tracking | Link to user sessions |
| Export | CSV download |

### L. 📈 Reports Module
**Purpose**: Analytics and reporting

| Report | Description |
|--------|-------------|
| Barangay Summary | Per-barangay statistics |
| User Activity | Login counts, contributions |
| Content Lifecycle | Status distribution |
| Revenue Trends | Monthly revenue charts |
| Subscription Reports | Plan distribution, churn |

### M. 🔔 Notifications Module
**Purpose**: Real-time alerts

| Feature | Description |
|---------|-------------|
| SignalR Integration | WebSocket-based delivery |
| Per-Barangay Groups | Targeted notifications |
| Types | Approvals, status changes, replies |
| Mark as Read | Notification management |

### N. ⚙️ Settings Module
**Purpose**: User preferences

| Feature | Description |
|---------|-------------|
| Profile Editing | Update personal info |
| Password Change | Security management |
| Profile Image | Avatar upload |

---

## 5. TRANSACTION FLOWS

### A. New Barangay Registration Flow

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   User      │    │   Browse    │    │  Register   │    │   Select    │
│  Visits     │───▶│   Plans     │───▶│  Barangay   │───▶│  Payment    │
│  Landing    │    │   Page      │    │   Details   │    │   Method    │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
                                                               │
                   ┌───────────────────────────────────────────┘
                   ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  Upload     │    │Super Admin  │    │  Barangay   │    │   Access    │
│  Payment    │───▶│  Verifies   │───▶│   Created   │───▶│   Granted   │
│  Proof      │    │  Payment    │    │ Admin User  │    │  Dashboard  │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
```

### B. Document Approval Workflow

```
┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│   Staff     │         │  Document   │         │   Admin     │
│  Uploads    │────────▶│  Created    │────────▶│  Reviews    │
│  Document   │         │  (PENDING)  │         │  Document   │
└─────────────┘         └─────────────┘         └──────┬──────┘
                                                       │
                              ┌─────────────────────────┤
                              │                         │
                              ▼                         ▼
                       ┌─────────────┐          ┌─────────────┐
                       │  APPROVED   │          │  REJECTED   │
                       │  (Published)│          │  (Feedback) │
                       └─────────────┘          └─────────────┘
```

### C. Payment Processing Flow

```
                          ┌─────────────────┐
                     ┌───▶│  Online Payment │───┐
                     │    │   (PayMongo)    │   │
┌─────────────┐      │    └─────────────────┘   │
│  Barangay   │      │                          │
│  Receives   │──────┤                          │
│  Invoice    │      │    ┌─────────────────┐   │    ┌─────────────────┐
└─────────────┘      │    │  Manual Payment │   │    │  Subscription   │
                     └───▶│  + Proof Upload │───┼───▶│   ACTIVATED     │
                          └────────┬────────┘   │    └─────────────────┘
                                   │            │
                                   ▼            │
                          ┌─────────────────┐   │
                          │  Admin Verifies │───┘
                          │     Payment     │
                          └─────────────────┘
```

### D. Knowledge Discussion Flow

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   User      │    │  Discussion │    │Other Users  │    │  SignalR    │
│  Creates    │───▶│   Created   │───▶│   View &    │───▶│  Notifies   │
│  Thread     │    │  (Active)   │    │   Reply     │    │   Author    │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
```

### E. Subscription Renewal Flow

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│ Background  │    │  Barangay   │    │   Admin     │    │   Make      │
│  Service    │───▶│  Notified   │───▶│  Receives   │───▶│  Payment    │
│ Checks      │    │  (Warning)  │    │   Invoice   │    │             │
│ Expiry      │    │             │    │             │    │             │
└─────────────┘    └─────────────┘    └─────────────┘    └──────┬──────┘
                                                                │
                   ┌────────────────────────────────────────────┘
                   ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  Payment    │    │Subscription │    │   Access    │
│  Verified   │───▶│  Extended   │───▶│  Continues  │
│             │    │             │    │             │
└─────────────┘    └─────────────┘    └─────────────┘
```

---

## 6. DATABASE SCHEMA

### Entity Relationship Overview

```
┌──────────────────────────────────────────────────────────────────────────┐
│                           CORE ENTITIES                                   │
└──────────────────────────────────────────────────────────────────────────┘

    ┌───────────────┐         ┌───────────────┐         ┌─────────────────┐
    │     Users     │◀───────▶│   Barangays   │◀────────│BarangaySubscr.  │
    │  (Business)   │         │   (Tenants)   │         │                 │
    │               │         │               │         │ • Status        │
    │ • Email       │         │ • Name        │         │ • StartDate     │
    │ • Role        │         │ • Province    │         │ • EndDate       │
    │ • BarangayId  │         │ • Municipality│         │ • PlanId        │
    └───────────────┘         └───────────────┘         └────────┬────────┘
                                                                 │
                              ┌───────────────┐                  │
                              │ Subscription  │◀─────────────────┘
                              │    Plans      │
                              │               │
                              │ • Name        │
                              │ • Price       │
                              │ • UserLimit   │
                              │ • Features    │
                              └───────────────┘

┌──────────────────────────────────────────────────────────────────────────┐
│                        KNOWLEDGE ENTITIES                                 │
└──────────────────────────────────────────────────────────────────────────┘

    ┌───────────────────┐     ┌───────────────────┐     ┌───────────────────┐
    │ KnowledgeRepository│     │     Policies      │     │  LessonsLearned   │
    │                    │     │                   │     │                   │
    │ • Title            │     │ • Title           │     │ • Title           │
    │ • Category         │     │ • Content         │     │ • Problem         │
    │ • FileUrl          │     │ • EffectiveDate   │     │ • ActionTaken     │
    │ • Status           │     │ • Status          │     │ • Result          │
    │ • BarangayId ─────▶│     │ • BarangayId ────▶│     │ • BarangayId ────▶│
    └───────────────────┘     └───────────────────┘     └───────────────────┘
              │
              ▼
    All knowledge entities have BarangayId for multi-tenant isolation

    ┌───────────────────┐     ┌───────────────────┐     ┌───────────────────┐
    │   BestPractices   │     │KnowledgeDiscussions│    │   Announcements   │
    │                   │     │                    │    │                   │
    │ • Title           │     │ • Title            │    │ • Title           │
    │ • Steps           │     │ • Content          │    │ • Priority        │
    │ • Rating          │     │ • LikesCount       │    │ • Status          │
    │ • IsFeatured      │     │ • RepliesCount     │    │ • IsPinned        │
    │ • BarangayId      │     │ • BarangayId       │    │ • BarangayId      │
    └───────────────────┘     └─────────┬──────────┘    └───────────────────┘
                                        │
                      ┌─────────────────┼─────────────────┐
                      ▼                                   ▼
              ┌───────────────┐                   ┌───────────────┐
              │Discussion     │                   │Discussion     │
              │Comments       │                   │Likes          │
              └───────────────┘                   └───────────────┘

┌──────────────────────────────────────────────────────────────────────────┐
│                        FINANCIAL ENTITIES                                 │
└──────────────────────────────────────────────────────────────────────────┘

    ┌───────────────────┐                     ┌───────────────────────┐
    │     Invoices      │◀────────────────────│SubscriptionPayments   │
    │                   │                     │                       │
    │ • InvoiceNumber   │                     │ • Amount              │
    │ • Amount          │                     │ • PaymentMethod       │
    │ • Status          │                     │ • Status              │
    │ • DueDate         │                     │ • ProofOfPaymentUrl   │
    │ • BarangayId      │                     │ • RejectionReason     │
    └───────────────────┘                     └───────────────────────┘

┌──────────────────────────────────────────────────────────────────────────┐
│                         SYSTEM ENTITIES                                   │
└──────────────────────────────────────────────────────────────────────────┘

    ┌───────────────────┐     ┌───────────────────┐     ┌───────────────────┐
    │    AuditLogs      │     │   Notifications   │     │PasswordResetReq.  │
    │                   │     │                   │     │                   │
    │ • Action          │     │ • Title           │     │ • Email           │
    │ • Module          │     │ • Message         │     │ • Token           │
    │ • TargetId        │     │ • Type            │     │ • Status          │
    │ • IpAddress       │     │ • IsRead          │     │ • ExpiresAt       │
    │ • UserAgent       │     │ • Link            │     │                   │
    │ • BarangayId      │     │ • UserId          │     │                   │
    └───────────────────┘     └───────────────────┘     └───────────────────┘
```

### Key Tables Summary

| Table | Records | Purpose | Key Fields |
|-------|---------|---------|------------|
| **Users** | Business users | Separate from Identity | Email, Role, BarangayId |
| **AspNetUsers** | Identity users | Authentication | Email, PasswordHash |
| **Barangays** | Tenant records | Organizations | Name, Municipality, Province |
| **SubscriptionPlans** | 3 | Plan definitions | Name, Price, UserLimit |
| **BarangaySubscriptions** | Per barangay | Active subscriptions | Status, StartDate, EndDate |
| **Invoices** | Per payment | Payment invoices | Amount, DueDate, Status |
| **SubscriptionPayments** | Per payment | Payment records | Method, Proof, Status |
| **KnowledgeRepository** | Documents | File storage | Title, FileUrl, Category |
| **Policies** | Policies | Policy documents | Title, Content, EffectiveDate |
| **LessonsLearned** | Lessons | PARC format | Problem, Action, Result |
| **BestPractices** | Practices | Shared practices | Steps, Rating, Implementations |
| **KnowledgeDiscussions** | Threads | Forum posts | Title, LikesCount, RepliesCount |
| **DiscussionComments** | Comments | Thread replies | Content, DiscussionId |
| **Announcements** | News | Official comms | Title, Priority, IsPinned |
| **AuditLogs** | 140+ | Activity tracking | Action, Module, IpAddress |
| **Notifications** | Per user | Alerts | Title, Type, IsRead |

---

## 7. SECURITY FEATURES

### Authentication

| Feature | Implementation |
|---------|----------------|
| **User Authentication** | ASP.NET Core Identity |
| **Password Storage** | PBKDF2 hashing (Identity default) |
| **Session Management** | 30-minute idle timeout |
| **Rate Limiting** | 5 login attempts per minute per IP |

### Authorization

| Feature | Implementation |
|---------|----------------|
| **Role-Based Access** | 5 roles with hierarchical permissions |
| **Tenant Isolation** | BarangayId filtering on all queries |
| **Action Filters** | `[DenyViewOnly]`, `[RequireRoles]` attributes |
| **Subscription Check** | `[RequireActiveSubscription]` blocks expired |

### Security Headers (Applied via Middleware)

```csharp
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: camera=(), microphone=(), geolocation=()
Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com ...
```

### Audit Logging

| Tracked Data | Description |
|--------------|-------------|
| **Who** | UserId, UserEmail, UserName |
| **What** | Action (Create, Update, Delete, Approve, etc.) |
| **Where** | Module (Documents, Policies, Users, etc.) |
| **When** | CreatedAt timestamp |
| **From Where** | IpAddress, UserAgent, SessionId |
| **On What** | TargetId, TargetType, TargetName |

### Data Protection

| Feature | Implementation |
|---------|----------------|
| **Soft Delete** | IsActive = false (never hard delete) |
| **Archive** | IsArchived flag for recoverable archiving |
| **CSRF Protection** | `[ValidateAntiForgeryToken]` on all POST |
| **API Rate Limiting** | 60 requests per minute per user |

---

## 8. EXTERNAL INTEGRATIONS

### PayMongo Payment Gateway

| Aspect | Details |
|--------|---------|
| **Purpose** | Philippine payment processing |
| **Integration** | REST API via HttpClient |
| **Supported Methods** | GCash, Maya, Credit/Debit Cards, Bank Transfer |

**API Endpoints Used**:
- `POST /v1/payment_intents` - Create payment
- `GET /v1/payment_intents/{id}` - Check status
- `POST /v1/checkout_sessions` - Create checkout URL

### SignalR (Real-time Notifications)

| Aspect | Details |
|--------|---------|
| **Hub URL** | `/notificationHub` |
| **Protocol** | WebSockets with fallback |
| **Groups** | `barangay_{id}`, `barangay_{id}_admins` |

**Events**:
- `ReceiveNotification` - New notification
- `NotificationCountUpdated` - Badge update
- `ContentStatusChanged` - Approval status change

---

## 9. KEY CONTROLLERS & SERVICES

### Controllers

| Controller | Lines | Purpose |
|------------|-------|---------|
| **HomeController** | 5000+ | Main application (97 actions) |
| **DashboardController** | 500+ | Dashboard views |
| **ReportsController** | 300+ | Analytics & reports |
| **DocumentsController** | 200+ | File operations |
| **PayMongoController** | 400+ | Payment API |
| **BaseAppController** | 100+ | Shared functionality |

### API Controllers

| Controller | Purpose |
|------------|---------|
| **NotificationsApiController** | Notification CRUD |
| **DocumentsApiController** | Document REST API |
| **AnnouncementsApiController** | Announcements REST API |
| **AuditLogsApiController** | Audit logs REST API |
| **SearchApiController** | Global search |

### Services

| Service | Interface | Purpose |
|---------|-----------|---------|
| **TenantService** | ITenantService | Multi-tenant context |
| **AuditService** | IAuditService | Audit logging |
| **DocumentService** | IDocumentService | Document operations |
| **SubscriptionService** | ISubscriptionService | Subscription management |
| **NotificationService** | - | Real-time notifications |
| **PayMongoService** | IPayMongoService | Payment gateway |
| **ReportingService** | IReportingService | Analytics |
| **SubscriptionExpiryService** | IHostedService | Background expiry check |

---

## 10. API ENDPOINTS

### Authentication Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Home/Login` | Login page |
| POST | `/Home/Login` | Process login |
| POST | `/Home/Logout` | Logout user |
| GET | `/Home/Register` | Registration page |
| POST | `/Home/Register` | Process registration |
| POST | `/Home/ForgotPassword` | Request password reset |

### Document Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Home/KnowledgeRepository` | List documents |
| POST | `/Home/CreateDocument` | Upload document |
| POST | `/Home/ApproveDocument` | Approve document |
| POST | `/Home/RejectDocument` | Reject document |
| POST | `/Home/ArchiveDocument` | Archive document |

### REST API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/notifications` | Get notifications |
| POST | `/api/notifications/mark-read` | Mark as read |
| POST | `/api/paymongo/create-checkout` | Create payment |
| GET | `/api/auditlogs` | Get audit logs |
| GET | `/api/search` | Global search |

---

## 11. SUBSCRIPTION PLANS

| Plan | Price | Users | Features |
|------|-------|-------|----------|
| **Basic** | ₱299/mo | 4 | View records, Add/manage records, Basic reports |
| **Professional** | ₱599/mo | 10 | All Basic + Announcements, Better reports, Activity logs |
| **Enterprise** | ₱999/mo | 20 | All Pro + Dashboard, Archive/restore, Detailed tracking |

### Feature Matrix by Plan

| Feature | Basic | Professional | Enterprise |
|---------|:-----:|:------------:|:----------:|
| Knowledge Repository | ✅ | ✅ | ✅ |
| Policies | ✅ | ✅ | ✅ |
| Lessons Learned | ✅ | ✅ | ✅ |
| Best Practices | ✅ | ✅ | ✅ |
| Discussions | ✅ | ✅ | ✅ |
| Basic Reports | ✅ | ✅ | ✅ |
| Announcements | ❌ | ✅ | ✅ |
| Enhanced Reports | ❌ | ✅ | ✅ |
| Activity Logs | ❌ | ✅ | ✅ |
| Dashboard | ❌ | ❌ | ✅ |
| Archive/Restore | ❌ | ❌ | ✅ |
| Detailed Tracking | ❌ | ❌ | ✅ |

---

## 12. DEFENSE Q&A PREPARATION

### Common Questions & Answers

**Q: What makes JAS-MINE different from other document management systems?**
> JAS-MINE is specifically designed for Philippine barangays with multi-tenant architecture, allowing multiple barangays to use the same system while maintaining complete data isolation. It includes knowledge management features like Lessons Learned and Best Practices that help preserve institutional knowledge.

**Q: How does the multi-tenant architecture work?**
> Each barangay has a unique BarangayId. When a user logs in, their BarangayId is stored in the session. All database queries automatically filter by this BarangayId using the `FilterByTenant()` extension method, ensuring users can only access their own barangay's data.

**Q: What security measures are implemented?**
> 1. ASP.NET Core Identity for authentication with password hashing
> 2. Role-based access control with 5 distinct roles
> 3. Session-based authentication with 30-minute timeout
> 4. Rate limiting on login (5 attempts/minute)
> 5. CSRF protection on all forms
> 6. Security headers (CSP, X-Frame-Options, etc.)
> 7. Comprehensive audit logging
> 8. Soft delete pattern (no data is permanently deleted)

**Q: How does the approval workflow work?**
> Content goes through a status cycle: Draft (optional) → Pending → Approved/Rejected. When staff creates content, it's marked as Pending. Barangay admins receive notifications and can approve or reject. All status changes are logged in the audit trail.

**Q: Explain the payment integration.**
> We use PayMongo, a Philippine payment gateway. Users can either:
> 1. Pay online via GCash, Maya, or card (redirects to PayMongo checkout)
> 2. Pay manually and upload proof, which admin verifies
> Once payment is verified, the subscription is activated.

**Q: How do you handle data backup and recovery?**
> We use soft delete pattern - IsActive flag instead of hard delete. We also have archive/restore functionality with IsArchived flag. All important entities have CreatedAt and UpdatedAt timestamps for tracking.

**Q: What happens when a subscription expires?**
> A background service (SubscriptionExpiryService) runs daily to check for expired subscriptions. When found, it updates the status to "Expired" and sends notifications. Users with expired subscriptions have limited access controlled by the `[RequireActiveSubscription]` filter.

**Q: How do real-time notifications work?**
> We use SignalR (WebSockets). Users join barangay-specific groups upon login. When an event occurs (new content, approval needed, status change), the server broadcasts to the relevant group. Clients receive and display notifications instantly.

**Q: How scalable is the system?**
> The multi-tenant architecture allows horizontal scaling - more barangays can be added without code changes. The database is normalized and indexed. Background services handle time-consuming tasks asynchronously.

---

## SUMMARY

**JAS-MINE** is a comprehensive, production-ready **Knowledge Management System** featuring:

✅ **Multi-tenant SaaS architecture** with complete data isolation  
✅ **5-tier role-based access control** from Super Admin to View-only  
✅ **10+ knowledge modules** (Documents, Policies, Lessons, Best Practices, etc.)  
✅ **Complete subscription & billing system** with PayMongo integration  
✅ **Real-time notifications** via SignalR  
✅ **Comprehensive audit logging** for compliance  
✅ **Modern security practices** (CSP, rate limiting, CSRF)  
✅ **Reporting & analytics** with CSV export  
✅ **Approval workflows** for quality control  
✅ **Archive/restore functionality** for data recovery  

---

*Document prepared for thesis defense - JAS-MINE IT15 Project*
