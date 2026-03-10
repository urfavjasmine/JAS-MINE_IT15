# JAS-MINE Knowledge Portal - Frontend Screenshots Guide
## Complete List with Labels and Descriptions

**System URL:** https://localhost:5292  
**Login Credentials:**
- Super Admin: `admin@jasmine.gov.ph` / `JasMine@1234`
- Barangay Admin: `brgyadmin@brgy.gov.ph` / `BrgyAdmin@1234`

---

# SECTION 1: AUTHENTICATION MODULE (4 Screens)

---

## Screen 1: Landing Page

**Label:** `Figure 1.1 - JAS-MINE Landing Page`

**URL:** `https://localhost:5292/Home/LandingPage`

**Description:**  
The Landing Page serves as the public-facing entry point to the JAS-MINE Knowledge Portal. This page introduces the system to potential users and displays the available subscription plans.

**Key Features Visible:**
- System logo and branding (JAS-MINE Knowledge Portal)
- Navigation menu with "Home", "Features", "Pricing", "Login" links
- Hero section with system tagline and call-to-action buttons
- Three subscription plan cards:
  - **Basic Plan** - ₱299/month (4 users)
  - **Professional Plan** - ₱599/month (10 users)
  - **Enterprise Plan** - ₱999/month (20 users)
- "Get Started" and "Learn More" buttons
- Footer with contact information

**How to Interact:**
1. Click "Login" to access the login page
2. Click "Get Started" or plan buttons to register a new barangay
3. Scroll down to view features and pricing details
4. Click subscription plan to proceed with registration

---

## Screen 2: Login Page

**Label:** `Figure 1.2 - User Login Page`

**URL:** `https://localhost:5292/Home/Login`

**Description:**  
The Login Page allows registered users to authenticate and access the system. It provides secure credential entry with password visibility toggle and links for account recovery.

**Key Features Visible:**
- JAS-MINE logo at the top
- Email input field with envelope icon
- Password input field with lock icon
- Eye icon button to toggle password visibility (show/hide)
- "Sign In" primary button
- "Forgot your password?" link
- "Back to Home" navigation link
- Footer with copyright notice

**How to Interact:**
1. Enter registered email address in the email field
2. Enter password in the password field
3. Click the eye icon to view/hide password while typing
4. Click "Sign In" to authenticate
5. If password forgotten, click "Forgot your password?" link
6. Click "Back to Home" to return to landing page

---

## Screen 3: Registration Page

**Label:** `Figure 1.3 - Barangay Registration Page`

**URL:** `https://localhost:5292/Home/Register`

**Description:**  
The Registration Page enables new barangays to create an account in the system. Users provide barangay information, administrator credentials, and select a subscription plan.

**Key Features Visible:**
- Multi-step registration form
- Barangay Information Section:
  - Barangay Name (required)
  - Municipality/City
  - Province
  - Region dropdown
  - Contact Email
  - Contact Phone
  - Address
- Administrator Account Section:
  - Full Name
  - Email Address
  - Password (with eye icon toggle)
  - Confirm Password (with eye icon toggle)
- Subscription Plan Selection dropdown
- Terms and Conditions checkbox
- "Register" submit button
- "Already have an account? Login" link

**How to Interact:**
1. Fill in all barangay details (name, location, contact)
2. Create administrator credentials (name, email, password)
3. Select desired subscription plan from dropdown
4. Check the Terms and Conditions agreement box
5. Click "Register" to submit registration
6. System will redirect to login after successful registration

---

## Screen 4: Forgot Password Page

**Label:** `Figure 1.4 - Password Reset Request Page`

**URL:** `https://localhost:5292/Home/ForgotPassword`

**Description:**  
The Forgot Password Page allows users who have forgotten their credentials to request a password reset. Requests are sent to the Super Admin for approval, after which a temporary password is assigned.

**Key Features Visible:**
- Information box explaining the reset process
- Email input field
- Reason/Justification textarea (optional)
- "Submit Request" button
- "Back to Login" link
- Note explaining that admin approval is required

**How to Interact:**
1. Enter the registered email address
2. Optionally provide a reason for the password reset
3. Click "Submit Request" to send the request
4. Wait for Super Admin approval notification
5. Once approved, use temporary password "Reset@123" to login
6. Change password after first login

---

# SECTION 2: DASHBOARD MODULE (3 Screens)

---

## Screen 5: Super Admin - System Dashboard

**Label:** `Figure 2.1 - Super Admin System Dashboard`

**URL:** `https://localhost:5292/Dashboard/System`

**Login Required:** `admin@jasmine.gov.ph` / `JasMine@1234`

**Description:**  
The System Dashboard provides Super Administrators with a comprehensive overview of the entire JAS-MINE platform. It displays system-wide statistics, recent activities, and quick access to administrative functions.

**Key Features Visible:**
- Welcome message with logged-in user email
- Left sidebar navigation with all admin modules
- Statistics Cards showing:
  - Total Barangays (registered in system)
  - Total Users (all roles)
  - Total Documents (uploaded)
  - Active Subscriptions
- Recent Activity feed
- Quick Action buttons
- Notification bell icon (top right)
- Dark/Light mode toggle

**How to Interact:**
1. View overall system statistics in the cards
2. Click sidebar menu items to navigate to different modules
3. Click notification bell to view pending items
4. Use quick actions for common tasks
5. Click user profile for settings/logout
6. Toggle dark/light mode with theme switcher

---

## Screen 6: Super Admin - System Monitoring

**Label:** `Figure 2.2 - System Monitoring Dashboard`

**URL:** `https://localhost:5292/Dashboard/SystemMonitoring`

**Login Required:** `admin@jasmine.gov.ph` / `JasMine@1234`

**Description:**  
The System Monitoring Dashboard provides detailed analytics and per-barangay performance metrics. Super Admins can monitor all barangays' content creation and subscription status.

**Key Features Visible:**
- Top Statistics Row:
  - Total Barangays
  - Total Users
  - Total Documents
  - Total Policies
  - Total Lessons Learned
  - Total Best Practices
- Growth indicators (new this month)
- **Per-Barangay Summary Table** with columns:
  - Barangay Name
  - Users count
  - Documents count
  - Policies count
  - Lessons count
  - Best Practices count
  - Subscription Plan (shows plan name or "No Plan")

**How to Interact:**
1. Review overall system statistics at the top
2. Check growth metrics to see monthly activity
3. Scroll through Per-Barangay Summary table
4. Identify barangays with low activity
5. Check subscription status for each barangay
6. Use data to make administrative decisions

---

## Screen 7: Barangay Admin - Barangay Dashboard

**Label:** `Figure 2.3 - Barangay Dashboard`

**URL:** `https://localhost:5292/Dashboard/Barangay`

**Login Required:** `brgyadmin@brgy.gov.ph` / `BrgyAdmin@1234`

**Description:**  
The Barangay Dashboard is the home screen for barangay-level users. It displays statistics specific to their barangay, recent uploads, and quick access to knowledge management functions.

**Key Features Visible:**
- Welcome message with user name and barangay
- Subscription status indicator (Active/Expired)
- Barangay-specific statistics:
  - Total Documents
  - Total Policies
  - Total Users
  - Pending Approvals
- Recent Documents section
- Recent Announcements
- Quick Upload button
- Left sidebar with barangay modules

**How to Interact:**
1. View your barangay's document/policy counts
2. Check subscription status at the top
3. Click "Quick Upload" to add new documents
4. View recent documents and announcements
5. Navigate using sidebar to access modules
6. Click pending counts to review items

---

# SECTION 3: ADMINISTRATION MODULE (4 Screens)

---

## Screen 8: Barangays Management

**Label:** `Figure 3.1 - Barangays Management`

**URL:** `https://localhost:5292/Home/BarangaysManagement`

**Login Required:** Super Admin only

**Description:**  
The Barangays Management page allows Super Admins to view and manage all registered barangays in the system. Each barangay's details, status, and contact information can be viewed and edited.

**Key Features Visible:**
- Page title "Barangays Management"
- Subtitle "Barangays are registered automatically when a subscription is approved"
- Statistics Cards:
  - Total Barangays
  - Active Barangays
  - Archived Barangays
- Search bar to filter barangays
- Barangays Table with columns:
  - Barangay (name with avatar)
  - Region
  - Municipality
  - Contact (email/phone)
  - Status (Active/Inactive badge)
  - Actions (Eye icon, kebab menu)
- Clickable rows to view details

**How to Interact:**
1. Use search bar to find specific barangays
2. Click any table row to open View Details modal
3. Click eye icon to view barangay details
4. Click three-dot menu for Edit/Archive options
5. View modal shows: Name, Municipality, Province, Region, Email, Phone, Address
6. Edit to update barangay information

---

## Screen 9: User Management

**Label:** `Figure 3.2 - User Management`

**URL:** `https://localhost:5292/Home/UserManagement`

**Login Required:** Super Admin or Barangay Admin

**Description:**  
The User Management page displays all system users with role-based filtering. Administrators can create, edit, disable, or archive user accounts based on their permissions.

**Key Features Visible:**
- Page title "User Management"
- Statistics Cards:
  - Total Users
  - Active Users
  - Inactive Users
  - By Role breakdown
- "Add User" button (for admins)
- Search bar
- Role filter dropdown
- Users Table with columns:
  - User (name with avatar)
  - Email
  - Role (color-coded badge)
  - Barangay (assigned)
  - Status (Active/Inactive)
  - Actions column
- Clickable rows to view details

**How to Interact:**
1. Click "Add User" to create new user
2. Use search to find users by name/email
3. Filter by role using dropdown
4. Click any row to view user details
5. Use action menu for Edit/Disable/Archive
6. Role badges: Super Admin (red), Barangay Admin (blue), etc.

---

## Screen 10: Audit Logs

**Label:** `Figure 3.3 - System Audit Logs`

**URL:** `https://localhost:5292/Home/AuditLogs`

**Login Required:** Super Admin or Barangay Admin

**Description:**  
The Audit Logs page provides a comprehensive activity trail of all user actions in the system. Every significant operation (login, create, update, delete, approve) is logged for security and compliance monitoring.

**Key Features Visible:**
- Page title "Audit Logs"
- Subtitle "Track all system activities and changes"
- "Print" and "Export as PDF" buttons
- Statistics Cards:
  - Total Entries
  - Approvals (green)
  - Creations (blue)
  - Rejections/Deletions (red)
- Activity Log section with:
  - Search bar
  - Module filter dropdown
  - Action filter dropdown
  - "Filter" button
- Logs Table with columns:
  - Timestamp
  - User (who performed action)
  - Action (Login, Create, Edit, Delete, Approve, etc.)
  - Module (Authentication, Documents, Users, etc.)
  - Target (affected item)
  - Actions (View, Archive)

**How to Interact:**
1. Use search to find specific log entries
2. Filter by Module (Documents, Users, etc.)
3. Filter by Action (Create, Delete, Login, etc.)
4. Click "Filter" to apply filters
5. Click any row or eye icon to view log details
6. Click "Print" for paper copy
7. Click "Export as PDF" to download logs

---

## Screen 11: Password Reset Requests

**Label:** `Figure 3.4 - Password Reset Requests`

**URL:** `https://localhost:5292/Home/PasswordRequests`

**Login Required:** Super Admin only

**Description:**  
The Password Reset Requests page shows all pending and processed password reset requests from users. Super Admins can approve or reject requests, with approved users receiving a temporary password.

**Key Features Visible:**
- Page title "Password Requests"
- Statistics Cards:
  - Total Requests
  - Pending
  - Approved
  - Rejected
- Requests Table with columns:
  - User Email
  - Request Date
  - Reason (if provided)
  - Status (Pending/Approved/Rejected badge)
  - Actions (Approve/Reject buttons)

**How to Interact:**
1. Review pending requests in the table
2. Click "Approve" to grant password reset
3. Click "Reject" to deny the request
4. Approved users receive temp password "Reset@123"
5. User must change password on next login
6. Filter by status if needed

---

# SECTION 4: SUBSCRIPTION MODULE (4 Screens)

---

## Screen 12: Subscription Plans

**Label:** `Figure 4.1 - Subscription Plans Management`

**URL:** `https://localhost:5292/Home/SubscriptionPlans`

**Login Required:** Super Admin only

**Description:**  
The Subscription Plans page displays all available subscription tiers with their pricing, features, and user limits. Super Admins can create new plans or modify existing ones.

**Key Features Visible:**
- Page title "Subscription Plans"
- "Create Plan" button
- Statistics Cards:
  - Total Plans
  - Active Plans
  - Subscribers count
- Plans Table with columns:
  - Plan Name
  - Price (₱ format)
  - Duration (months)
  - User Limit
  - Features
  - Status (Active/Inactive)
  - Actions
- Clickable rows to view full details

**How to Interact:**
1. Click "Create Plan" to add new subscription tier
2. Click any row to view plan details
3. View features list for each plan
4. Edit plans via action menu
5. Deactivate plans that are no longer offered
6. Cannot delete plans with active subscriptions

---

## Screen 13: Barangay Subscriptions

**Label:** `Figure 4.2 - Barangay Subscriptions`

**URL:** `https://localhost:5292/Home/BarangaySubscriptions`

**Login Required:** Super Admin only

**Description:**  
The Barangay Subscriptions page manages all barangay subscription assignments. Shows which barangays have active, pending, or expired subscriptions.

**Key Features Visible:**
- Page title "Barangay Subscriptions"
- "Assign Plan" button
- Statistics Cards:
  - Total subscriptions
  - Active (green)
  - Pending (yellow)
  - Expired (red)
  - Cancelled (gray)
- Search and status filter
- Subscriptions Table with columns:
  - Barangay Name
  - Plan Name
  - Start Date
  - End Date
  - Status (color-coded badge)
  - Actions (Cancel, Archive)
- Clickable rows to view details

**How to Interact:**
1. Click "Assign Plan" to assign subscription to barangay
2. Click any row to view subscription details
3. Filter by status (Active, Pending, Expired)
4. Cancel active subscriptions if needed
5. Archive old subscription records
6. Modal shows: Barangay, Plan, Dates, Status

---

## Screen 14: Subscription Payments

**Label:** `Figure 4.3 - Subscription Payments`

**URL:** `https://localhost:5292/Home/SubscriptionPayments`

**Login Required:** Super Admin only

**Description:**  
The Subscription Payments page tracks all payment transactions for subscriptions. Shows payment status, amounts, and allows verification of pending payments.

**Key Features Visible:**
- Page title "Subscription Payments"
- Subtitle "Payments are recorded automatically when a barangay completes payment via PayMongo"
- Statistics Cards:
  - Total Payments
  - Total Collected (₱ amount)
  - Awaiting Verification (yellow)
  - Approved (green)
  - Rejected (red)
  - Failed (red)
- Payments Table with columns:
  - Barangay
  - Plan
  - Amount (₱)
  - Date
  - Payment Method
  - Status (badge)
  - Actions (Approve/Reject for pending)
- Clickable rows to view payment details

**How to Interact:**
1. Click statistic cards to filter by status
2. Click any row to view payment details
3. For "Pending Verification" payments:
   - Click green checkmark to Approve
   - Click red X to Reject
4. Approved payments activate subscription
5. View modal shows full payment information
6. Archive completed payments

---

## Screen 15: Payment Verification

**Label:** `Figure 4.4 - Payment Verification Queue`

**URL:** `https://localhost:5292/Home/PaymentVerification`

**Login Required:** Super Admin only

**Description:**  
The Payment Verification page is a dedicated queue for payments awaiting manual verification. Displays proof of payment and allows admins to verify payment validity.

**Key Features Visible:**
- Page title "Payment Verification"
- Pending verification count
- Verification Queue Table:
  - Barangay Name
  - Plan Selected
  - Amount
  - Payment Date
  - Reference Number
  - Proof of Payment (if uploaded)
  - Actions (Verify/Reject)
- Empty state message when no pending verifications

**How to Interact:**
1. Review each pending payment
2. Check payment proof/reference
3. Click "Verify" to confirm valid payment
4. Click "Reject" if payment is invalid
5. Verified payments auto-activate subscription
6. Rejected payments notify the barangay

---

# SECTION 5: KNOWLEDGE MANAGEMENT MODULE (5 Screens)

---

## Screen 16: Knowledge Repository

**Label:** `Figure 5.1 - Knowledge Repository`

**URL:** `https://localhost:5292/Home/KnowledgeRepository`

**Login Required:** Any authenticated user

**Description:**  
The Knowledge Repository is the central document management system. Users can upload, categorize, search, and manage knowledge documents. Documents go through an approval workflow.

**Key Features Visible:**
- Page title "Knowledge Repository"
- "Upload Document" button
- Statistics Cards:
  - Total Documents
  - Approved
  - Pending Approval
  - Archived
- Search bar
- Category filter dropdown
- Status filter dropdown
- Documents Table with columns:
  - Document (title with file icon)
  - Category
  - Author/Uploaded By
  - Date
  - Status (Pending/Approved/Rejected)
  - Version
  - Actions (View, Download, Edit, Archive)
- Clickable rows to view document details

**How to Interact:**
1. Click "Upload Document" to add new file
2. Fill in: Title, Category, Tags, Description, File
3. Search documents by name or content
4. Filter by Category or Status
5. Click row to view document details
6. Click download icon to save file
7. Admins can Approve/Reject pending documents

---

## Screen 17: Policies & Procedures

**Label:** `Figure 5.2 - Policies & Procedures Management`

**URL:** `https://localhost:5292/Home/PoliciesManagement`

**Login Required:** Any authenticated user

**Description:**  
The Policies & Procedures page manages official barangay policies. Each policy has version control, status tracking, and approval workflow.

**Key Features Visible:**
- Page title "Policies & Procedures"
- "Create Policy" button
- Statistics Cards:
  - Total Policies
  - Draft
  - Under Review
  - Approved
  - Superseded
- Search and filter controls
- Policies Table with columns:
  - Policy Title
  - Description (truncated)
  - Status (color-coded badge)
  - Version number
  - Author
  - Last Updated
  - Actions

**How to Interact:**
1. Click "Create Policy" to add new policy
2. Fill in: Title, Description, Category, Content
3. Save as Draft or Submit for Review
4. Reviewers can Approve or Request Changes
5. Approved policies become official
6. Supersede old versions when updating
7. Archive deprecated policies

---

## Screen 18: Lessons Learned

**Label:** `Figure 5.3 - Lessons Learned Repository`

**URL:** `https://localhost:5292/Home/LessonsLearned`

**Login Required:** Any authenticated user

**Description:**  
The Lessons Learned page is a knowledge base for documenting organizational experiences. Users record what worked, what didn't, and recommendations for future reference.

**Key Features Visible:**
- Page title "Lessons Learned"
- "Add Lesson" button
- Statistics Cards:
  - Total Lessons
  - This Month
  - Categories breakdown
- Filter by Category
- Lessons Table with columns:
  - Title
  - Category
  - Description (preview)
  - Author
  - Date Added
  - Actions (View, Edit, Archive)
- Clickable rows for details

**How to Interact:**
1. Click "Add Lesson" to record new lesson
2. Fill in: Title, Category, What Happened, Lessons, Recommendations
3. Search existing lessons
4. Filter by category
5. Click row to read full lesson content
6. Edit your own lessons
7. Archive outdated lessons

---

## Screen 19: Best Practices

**Label:** `Figure 5.4 - Best Practices Repository`

**URL:** `https://localhost:5292/Home/BestPractices`

**Login Required:** Any authenticated user

**Description:**  
The Best Practices page collects proven methods and successful approaches. Users share what works well so others can adopt similar practices.

**Key Features Visible:**
- Page title "Best Practices"
- "Add Best Practice" button
- Category filter
- Best Practices list/cards showing:
  - Title
  - Category
  - Description
  - Implementation Steps
  - Expected Outcomes
  - Author
  - Date
  - Actions

**How to Interact:**
1. Click "Add Best Practice" to share new practice
2. Fill in: Title, Category, Description, Steps, Outcomes
3. Browse existing practices
4. Filter by category
5. Click to view full details
6. Implement practices in your barangay
7. Share successful outcomes

---

## Screen 20: Shared Documents

**Label:** `Figure 5.5 - Shared Documents`

**URL:** `https://localhost:5292/Home/SharedDocuments`

**Login Required:** Any authenticated user

**Description:**  
The Shared Documents page displays official documents shared by the Super Admin to all barangays. These include templates, guidelines, and reference materials.

**Key Features Visible:**
- Page title "Shared Documents"
- Description "Official documents shared to all barangays"
- "Share Document" button (Super Admin only)
- Search bar
- Category filter
- Documents Table with columns:
  - Document Name
  - Category
  - Description
  - Shared By
  - Date Shared
  - Actions (View, Download)
- Clickable rows to view details

**How to Interact:**
1. Browse available shared documents
2. Search by document name
3. Filter by category
4. Click row to view document details
5. Click download to save locally
6. Super Admin: Click "Share Document" to upload
7. Documents visible to all barangays

---

# SECTION 6: COMMUNICATION MODULE (2 Screens)

---

## Screen 21: Announcements

**Label:** `Figure 6.1 - Announcements Management`

**URL:** `https://localhost:5292/Home/Announcements`

**Login Required:** Any authenticated user

**Description:**  
The Announcements page allows barangay administrators to create and publish announcements visible to all users in their barangay. Supports priority levels and scheduling.

**Key Features Visible:**
- Page title "Announcements"
- "Create Announcement" button
- Statistics:
  - Total Announcements
  - Published
  - Draft
- Announcements List showing:
  - Title
  - Content preview
  - Priority (Normal/High/Urgent badge)
  - Author
  - Publish Date
  - Status (Published/Draft)
  - Actions (View, Edit, Delete)

**How to Interact:**
1. Click "Create Announcement" to add new
2. Fill in: Title, Content, Priority, Publish Date
3. Save as Draft or Publish immediately
4. Edit existing announcements
5. Delete outdated announcements
6. High priority announcements appear prominently
7. All barangay users receive notifications

---

## Screen 22: Knowledge Sharing / Discussions

**Label:** `Figure 6.2 - Knowledge Sharing Forum`

**URL:** `https://localhost:5292/Home/KnowledgeSharing`

**Login Required:** Any authenticated user

**Description:**  
The Knowledge Sharing page is a discussion forum where all users can post topics, share ideas, and engage in conversations. Supports categories, likes, comments, and sharing.

**Key Features Visible:**
- Page title "Knowledge Sharing"
- "Quick Post" textarea for fast posting
- "Full Post" button for detailed posts
- Category filter buttons:
  - All Categories
  - General
  - Health
  - Environment
  - Youth
  - Education
  - Governance
  - Finance
- Discussion Cards showing:
  - Author avatar and name
  - Post title
  - Content preview
  - Category badge (color-coded)
  - Like count and button
  - Comment count
  - Share button
  - Timestamp
  - Options menu (View, Edit, Delete)
- Comments section for each post

**How to Interact:**
1. Type in Quick Post box for simple posts
2. Click "Full Post" for detailed post with title and category
3. Click category buttons to filter discussions
4. Click heart icon to like a post
5. Click comment icon to view/add comments
6. Click share icon to share post
7. Click three-dot menu for Edit/Delete
8. All logged-in users can post and comment

---

# SECTION 7: REPORTS MODULE (1 Screen)

---

## Screen 23: Reports & Analytics

**Label:** `Figure 7.1 - Reports & Analytics Dashboard`

**URL:** `https://localhost:5292/Home/ReportsAnalytics`

**Login Required:** Admin users

**Description:**  
The Reports & Analytics page provides visual dashboards showing document uploads, user activity, policy status, and performance trends over time. Supports data export.

**Key Features Visible:**
- Page title "Reports & Analytics"
- "Export PDF" and "Export CSV" buttons
- Date range selector
- Charts and Graphs:
  - Document Uploads Over Time (line chart)
  - Policy Status Breakdown (pie chart)
  - User Activity Trends (bar chart)
  - Category Distribution (donut chart)
- Summary Tables:
  - Top Contributors
  - Most Viewed Documents
  - Activity by Module
- Key Metrics:
  - Total Documents
  - Total Policies
  - Active Users
  - Growth Rate

**How to Interact:**
1. Select date range for report period
2. View visual charts and trends
3. Hover over charts for detailed values
4. Click "Export PDF" for printable report
5. Click "Export CSV" for spreadsheet data
6. Filter by category or module
7. Compare month-over-month growth

---

# SECTION 8: SETTINGS (1 Screen)

---

## Screen 24: Settings

**Label:** `Figure 8.1 - System Settings`

**URL:** `https://localhost:5292/Home/Settings`

**Login Required:** Admin users

**Description:**  
The Settings page allows administrators to configure system preferences, manage their profile, and update account settings.

**Key Features Visible:**
- Page title "Settings"
- Profile Section:
  - Profile photo
  - Full Name
  - Email (read-only)
  - Role (read-only)
- Change Password Section:
  - Current Password
  - New Password
  - Confirm New Password
- Notification Preferences:
  - Email notifications toggle
  - System alerts toggle
- Theme Preferences:
  - Light/Dark mode toggle
- "Save Changes" button

**How to Interact:**
1. Update profile information
2. Change password using the form
3. Toggle notification preferences
4. Switch between light/dark themes
5. Click "Save Changes" to apply updates
6. Changes take effect immediately

---

# SCREENSHOT CHECKLIST

| # | Screen Name | Label | URL Path |
|---|-------------|-------|----------|
| 1 | Landing Page | Figure 1.1 | /Home/LandingPage |
| 2 | Login Page | Figure 1.2 | /Home/Login |
| 3 | Registration Page | Figure 1.3 | /Home/Register |
| 4 | Forgot Password | Figure 1.4 | /Home/ForgotPassword |
| 5 | Super Admin Dashboard | Figure 2.1 | /Dashboard/System |
| 6 | System Monitoring | Figure 2.2 | /Dashboard/SystemMonitoring |
| 7 | Barangay Dashboard | Figure 2.3 | /Dashboard/Barangay |
| 8 | Barangays Management | Figure 3.1 | /Home/BarangaysManagement |
| 9 | User Management | Figure 3.2 | /Home/UserManagement |
| 10 | Audit Logs | Figure 3.3 | /Home/AuditLogs |
| 11 | Password Requests | Figure 3.4 | /Home/PasswordRequests |
| 12 | Subscription Plans | Figure 4.1 | /Home/SubscriptionPlans |
| 13 | Barangay Subscriptions | Figure 4.2 | /Home/BarangaySubscriptions |
| 14 | Subscription Payments | Figure 4.3 | /Home/SubscriptionPayments |
| 15 | Payment Verification | Figure 4.4 | /Home/PaymentVerification |
| 16 | Knowledge Repository | Figure 5.1 | /Home/KnowledgeRepository |
| 17 | Policies & Procedures | Figure 5.2 | /Home/PoliciesManagement |
| 18 | Lessons Learned | Figure 5.3 | /Home/LessonsLearned |
| 19 | Best Practices | Figure 5.4 | /Home/BestPractices |
| 20 | Shared Documents | Figure 5.5 | /Home/SharedDocuments |
| 21 | Announcements | Figure 6.1 | /Home/Announcements |
| 22 | Knowledge Sharing | Figure 6.2 | /Home/KnowledgeSharing |
| 23 | Reports & Analytics | Figure 7.1 | /Home/ReportsAnalytics |
| 24 | Settings | Figure 8.1 | /Home/Settings |

---

# TIPS FOR TAKING SCREENSHOTS

1. **Resolution:** Use 1920x1080 for consistency
2. **Browser:** Chrome or Edge recommended
3. **Clear Data:** Clear cache before screenshots
4. **Sample Data:** Ensure tables have some data visible
5. **Highlight:** Use red boxes to highlight key features
6. **Labels:** Add figure labels in your document
7. **Consistent:** Keep same zoom level throughout

**Tool:** Press `Win + Shift + S` on Windows for Snipping Tool

---

*JAS-MINE IT15 Capstone Project - Frontend Screenshot Guide*
