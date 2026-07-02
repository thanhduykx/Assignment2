const translations = {
  en: {
    "nav.documents": "Documents",
    "nav.chat": "Chat",
    "nav.logout": "Logout",
    "nav.login": "Login",
    "nav.register": "Create account",
    "shell.portal": "Learning Portal",
    "shell.chatbot": "Chatbot",
    "shell.courses": "Courses",
    "shell.documents": "Document Repository",
    "shell.users": "Users",
    "shell.help": "Help",
    "shell.search": "Search the system...",
    "shell.notifications": "Notifications",
    "shell.history": "History",
    "shell.changePassword": "Change password",
    "admin.totalUsers": "Total users",
    "admin.usersShown": "shown",
    "admin.admins": "Admins",
    "admin.fullSystemAccess": "Full system access",
    "admin.lecturers": "Lecturers",
    "admin.manageSubjects": "Manage assigned subjects",
    "admin.students": "Students",
    "admin.readChatAccess": "Read and chat access",
    "admin.subjects": "Subjects",
    "admin.subjectsAssigned": "assigned",
    "admin.directory": "Directory",
    "admin.usersUnit": "users",
    "admin.searchDirectory": "Search / Tìm kiếm",
    "admin.searchDirectoryPlaceholder": "Name, email, provider, subject...",
    "admin.roleFilter": "Role / Vai trò",
    "admin.allRoles": "All roles",
    "admin.filter": "Filter",
    "admin.clear": "Clear",
    "admin.noMatchingUsers": "No matching users.",
    "admin.noMatchingUsersHint": "Clear filters or create a new account from the command panel.",
    "admin.thUser": "User",
    "admin.thCreated": "Created",
    "admin.thRole": "Role / Vai trò",
    "admin.thSubjects": "Subjects",
    "admin.unassignedSubject": "Chưa đăng ký môn",
    "admin.allIndexedDocs": "All indexed documents",
    "admin.notApplicable": "Not applicable",
    "admin.delete": "Delete",
    "admin.lecturerManagement": "Lecturer management",
    "admin.lecturerTableHeading": "Lecturer Table",
    "admin.lecturersUnit": "lecturers",
    "admin.noLecturersFound": "No lecturers found.",
    "admin.createLecturerHint": "Create a lecturer account first.",
    "admin.thName": "NAME",
    "admin.subjectLeaderSuffix": " (Subject leader)",
    "admin.teachingLecturerRole": "Teaching lecturer",
    "admin.leaderRole": "Subject leader",
    "admin.noSubjectsAvailable": "No subjects available",
    "admin.selectSubjectAssign": "Select subject to assign",
    "admin.assignButton": "Assign",
    "admin.studentManagement": "Student management",
    "admin.studentTableHeading": "Student Table",
    "admin.studentsUnit": "students",
    "admin.noStudentsFound": "No students found.",
    "admin.createStudentHint": "Create a student account first.",
    "admin.roleStudent": "Student",
    "admin.roleLecturer": "Lecturer",
    "admin.roleAdmin": "Admin",
    "admin.createAccountHeading": "Create user",
    "admin.fullName": "Full name",
    "admin.email": "Email",
    "admin.password": "Password",
    "admin.role": "Role",
    "admin.subjectsForLecturer": "Subjects for lecturer",
    "admin.createUserBtn": "Create user",
    "documents.shellTitle": "Course Details",
    "documents.backToCatalog": "Back to course catalog",
    "documents.eyebrow": "Course documents",
    "documents.defaultDescription": "Manage chapters, files and lecture links for this subject.",
    "documents.documentsUnit": "documents",
    "documents.chaptersUnit": "chapters",
    "documents.editSubjectInfo": "Edit subject info",
    "documents.subjectCode": "Subject code",
    "documents.subjectDesc": "Description",
    "documents.deleteSubject": "Delete subject",
    "documents.saveSubject": "Save subject",
    "documents.addChapter": "Add chapter",
    "documents.chapterTitle": "Chapter title",
    "documents.chapterTitlePlaceholder": "e.g. Chapter 1 / Unit 1.1",
    "documents.sortOrder": "Sort order",
    "documents.addChapterBtn": "Add chapter",
    "documents.addDocument": "Add document",
    "documents.documentFile": "Document file",
    "documents.orLectureUrl": "Or lecture URL",
    "documents.indexDocumentBtn": "Index document",
    "documents.chaptersAndDocuments": "Chapters and documents",
    "documents.noChapters": "This subject has no chapters yet.",
    "documents.noChaptersHint": "Add a chapter first, then upload documents into it.",
    "documents.updateBtn": "Update",
    "documents.deleteBtn": "Delete",
    "documents.addFileTo": "Add file to ",
    "documents.chapterLabel": "Chapter",
    "documents.emptyChapter": "No documents in this chapter yet.",
    "documents.previewBtn": "Preview",
    "documents.editBtn": "Edit",
    "documents.reindexBtn": "Re-index",
    "documents.deleteDocBtn": "Delete",
    "documents.indexedContent": "Indexed content",
    "documents.previewTitle": "Document",
    "documents.loadingContent": "Loading indexed content...",
    "documents.reindexConfirm": "Re-index this document?",
    "documents.deleteConfirm": "Delete this document from index? This cannot be undone.",
    "documents.deleteChapterConfirm": "Delete this chapter? This cannot be undone.",
    "documents.deleteSubjectConfirm": "Delete this subject and all chapters inside? This cannot be undone.",
    "documents.shellTitle": "Course details",
    "documents.backToCatalog": "Back to course list",
    "documents.eyebrow": "Course documents",
    "documents.defaultDescription": "Manage chapters, files and lecture links for this course.",
    "documents.documentsUnit": "documents",
    "documents.chaptersUnit": "chapters",
    "documents.editSubjectInfo": "Edit course information",
    "documents.subjectCode": "Course code",
    "documents.subjectDesc": "Description",
    "documents.deleteSubject": "Delete course",
    "documents.saveSubject": "Save course",
    "documents.addChapter": "Add chapter",
    "documents.chapterTitle": "Chapter title",
    "documents.chapterTitlePlaceholder": "Ex: Chapter 1 / Week 1",
    "documents.sortOrder": "Sort order",
    "documents.addChapterBtn": "Add chapter",
    "documents.addDocument": "Add document",
    "documents.documentFile": "Document file",
    "documents.orLectureUrl": "Or lecture URL",
    "documents.indexDocumentBtn": "Index document",
    "documents.chaptersAndDocuments": "Chapters and documents",
    "documents.noChapters": "This course has no chapters.",
    "documents.noChaptersHint": "Add a chapter first, then upload documents into the correct category.",
    "documents.updateBtn": "Update",
    "documents.deleteBtn": "Delete",
    "documents.addFileTo": "Add file to ",
    "documents.chapterLabel": "Chapter",
    "documents.emptyChapter": "No documents in this chapter.",
    "documents.previewBtn": "Preview",
    "documents.editBtn": "Edit",
    "documents.reindexBtn": "Re-index",
    "documents.deleteDocBtn": "Delete",
    "documents.indexedContent": "Indexed content",
    "documents.previewTitle": "Document",
    "documents.loadingContent": "Loading indexed content...",
    "documents.reindexConfirm": "Re-index this document?",
    "documents.deleteConfirm": "Delete this document from the index? This action cannot be undone.",
    "documents.deleteChapterConfirm": "Delete this chapter? This action cannot be undone.",
    "documents.deleteSubjectConfirm": "Delete this course and all its chapters? This action cannot be undone.",
    "documents.navCatalog": "Course catalog",
    "documents.catalogEmptyDesc": "List of created chapters",
    "documents.emptyCatalogTitle": "No course catalog yet.",
    "documents.emptyCatalogHint": "Create a course first, then add chapters to upload documents according to standard categories.",
    "documents.createFirstChapterBtn": "Create first chapter",
    "documents.uploadTitle": "Upload documents",
    "documents.docInfo": "Document information",
    "documents.chooseFileBtn": "Choose file",
    "documents.url": "Lecture URL",
    "documents.enterUrl": "Enter lecture URL",
    "admin.usersTitle": "Users",
    "admin.navDirectory": "Directory",
    "admin.navOnlineUsers": "Online users",
    "admin.navLecturerTable": "Lecturer table",
    "admin.navStudentTable": "Student table",
    "admin.navCreateAccount": "Create account",
    "admin.navImportExcel": "Import from Excel",
    "admin.navCreateSubject": "Create subject",
    "admin.navSubjectManagement": "Subject management",
    "admin.noneLeader": "None",
    "admin.subjectCode": "Subject code",
    "admin.description": "Description",
    "admin.optional": "Optional",
    "admin.subjectList": "Subject List",
    "admin.subjectsUnit": "subjects",
    "admin.noSubjects": "No subjects found.",
    "admin.createSubjectPrompt": "Create a subject above.",
    "admin.thSubject": "SUBJECT",
    "admin.thLeader": "LEADER",
    "admin.thStudents": "STUDENTS",
    "admin.thStatus": "STATUS",
    "admin.thActions": "ACTIONS",
    "admin.active": "Active",
    "admin.inactive": "Inactive",
    "admin.deactivate": "Deactivate",
    "admin.activate": "Activate",
    "courses.eyebrow": "Course workspace",
    "courses.description": "Open a course workspace to view documents, chapters, and subject chat.",
    "courses.emptyTitle": "No course workspace is available.",
    "courses.emptyHint": "Upload and index course documents first, then come back here.",
    "courses.noDescription": "No description yet.",
    "courses.unassigned": "Unassigned",
    "courses.chaptersUnit": "chapters",
    "courseWorkspace.backToCourses": "Back to courses",
    "courseWorkspace.defaultDescription": "Course workspace for documents, chapters, and subject chat.",
    "courseWorkspace.chatCourse": "Chat this course",
    "courseWorkspace.manageDocuments": "Manage documents",
    "courseWorkspace.documents": "Documents",
    "courseWorkspace.chapters": "Chapters",
    "courseWorkspace.lecturer": "Lecturer",
    "courseWorkspace.noEmail": "No owner email",
    "courseWorkspace.courseMap": "Course map",
    "courseWorkspace.chaptersAndDocs": "Chapters and documents",
    "courseWorkspace.emptyChaptersTitle": "No chapter has indexed content yet.",
    "courseWorkspace.emptyChaptersHint": "Upload documents or ask the lecturer to add course material.",
    "courseWorkspace.docsUnit": "docs",
    "courseWorkspace.chunksUnit": "chunks",
    "admin.importExcelEyebrow": "Excel",
    "admin.importExcelTitle": "Import from Excel",
    "admin.excelFile": "Excel file",
    "admin.excelImportHint": "The system automatically extracts the full name and email columns.",
    "admin.defaultRole": "Default role",
    "admin.defaultLecturerSubjects": "Default subjects for lecturers",
    "admin.noUnassignedSubjects": "No unassigned subjects",
    "admin.importUsers": "Import users",
    "admin.importWelcomeNote": "Welcome emails will include each user's role and subject access.",
    "admin.confirmImportUsers": "Create these users in bulk and send welcome emails?",
    "admin.confirmRoleChange": "Change {user} to {role}?",
    "admin.roleChangeTitle": "Changing a role asks for confirmation and saves automatically.",
    "onlineUsers.title": "Online users",
    "onlineUsers.connecting": "Connecting...",
    "onlineUsers.loading": "Loading online users...",
    "onlineUsers.empty": "No active users right now.",
    "onlineUsers.activeNow": "Active now",
    "onlineUsers.realtimeNote": "Realtime presence",
    "onlineUsers.statusHelp": "This list updates when users connect or leave.",
    "onlineUsers.summaryOne": "1 online user (active now)",
    "onlineUsers.summaryMany": "{count} online users (active now)",
    "onlineUsers.youAreOnline": "You are online",
    "onlineUsers.online": "Online",
    "onlineUsers.tabs": "{count} tabs",
    "onlineUsers.expand": "Expand online users",
    "onlineUsers.collapse": "Collapse online users",
    "documents.shellTitle": "Document Repository",
    "documents.manageTitle": "Document repository management",
    "documents.manageSubtitle": "Upload learning materials, extract content, and index them so the chatbot can answer with sources.",
    "documents.uploadKicker": "Upload",
    "documents.dropTitle": "Drop documents here",
    "documents.chooseFile": "Choose a file or drag it here",
    "documents.urlRenderHint": "SPA/React/Vue pages will be rendered before DOM extraction.",
    "documents.storage": "Storage",
    "documents.uploadedSize": "Uploaded size",
    "documents.totalUploaded": "Total uploaded",
    "documents.documentList": "Document list",
    "documents.showing": "Showing",
    "documents.fileName": "File name",
    "documents.status": "Status",
    "documents.uploadDate": "Upload date",
    "documents.indexedStatus": "Indexed",
    "documents.emptyTitle": "No documents yet.",
    "documents.emptyHint": "Upload a syllabus or lecture URL to start source-grounded Q&A.",
    "assistant.open": "Open chat page",
    "assistant.hidden": "Hi, I am your chatbot assistant for finding information faster.",
    "documents.title": "Course document repository",
    "documents.subtitle": "Manage uploaded learning materials, extract content, and index them for the dedicated chat page.",
    "documents.openChat": "Open chat",
    "documents.statsAria": "Document repository statistics",
    "documents.statsDocuments": "Documents",
    "documents.statsIndexed": "Indexed",
    "documents.statsProcessing": "Processing",
    "documents.uploadTitle": "Upload document",
    "documents.uploadSubtitle": "PDF, DOCX, PPTX, TXT, or a lecture page URL will be extracted, chunked, and embedded automatically.",
    "documents.subject": "Subject",
    "documents.chapter": "Chapter",
    "documents.source": "Source",
    "documents.subjectPlaceholder": "Example: IOT102",
    "documents.chapterPlaceholder": "Example: Chapter 1 or Week 1",
    "documents.dropzoneTitle": "Drag and drop a document or click to choose a file",
    "documents.dropzoneDefault": "Supports PDF, DOCX, PPTX, TXT",
    "documents.or": "or",
    "documents.url": "Lecture page URL",
    "documents.urlPlaceholder": "https://example.com/react-vue-lecture",
    "documents.urlHint": "SPA/React/Vue pages will be rendered with Playwright before DOM extraction.",
    "documents.submit": "Upload and index",
    "documents.indexedTitle": "Indexed documents",
    "documents.filesUnit": "files",
    "documents.empty": "No documents yet. Upload course materials to start asking source-grounded questions.",
    "documents.view": "View",
    "documents.navList": "Document list",
    "documents.navUpload": "Upload & index",
    "documents.navUpload": "Upload & index",
    "documents.navCatalog": "Course catalog",
    "documents.eyebrow": "Learning content operations",
    "documents.kpiTotal": "Total",
    "documents.kpiFailed": "Failed",
    "documents.selectSubject": "Select subject",
    "documents.noSubjectAssigned": "No subjects assigned to this account yet.",
    "documents.reindexStale": "Re-index stale",
    "documents.search": "Search",
    "documents.searchPlaceholder": "File, subject, chapter, uploader...",
    "documents.allSubjects": "All subjects",
    "documents.allStatus": "All status",
    "documents.filter": "Filter",
    "documents.clear": "Clear",
    "documents.uploader": "Uploader",
    "documents.subjectChapter": "Subject / Chapter",
    "documents.chunksSize": "Chunks / Size",
    "documents.chunksUnit": "chunks",
    "documents.unknown": "Unknown",
    "documents.actions": "Actions",
    "documents.edit": "Edit",
    "documents.reindex": "Re-index",
    "documents.delete": "Delete",
    "chat.sessionsAria": "Chat session history",
    "chat.documents": "Documents",
    "chat.newSession": "New session",
    "chat.history": "Session history",
    "chat.sessionsUnit": "sessions",
    "chat.messagesUnit": "messages",
    "chat.noSessions": "No sessions yet.",
    "chat.mainAria": "Document chat",
    "chat.title": "Document chat",
    "chat.subtitle": "Ask questions based on indexed documents. Questions outside the document scope will be marked as insufficient data.",
    "chat.headerKicker": "Course assistant",
    "chat.headerTitle": "Introduction to AI",
    "chat.headerSubtitle": "Ask questions from indexed documents. If the data is insufficient, the chatbot must report missing sources instead of guessing.",
    "chat.currentSession": "Current session",
    "chat.emptyTitle": "Start with a specific question",
    "chat.emptyText": "Choose a suggestion below or type your question. If the documents do not contain enough data, the chatbot will say so instead of guessing.",
    "chat.suggestionsAria": "Question suggestions",
    "chat.welcome": "Hi, ask me about uploaded documents. I will keep the answer short and show sources when I have enough data.",
    "chat.placeholder": "Ask about a subject, chapter, or indexed document...",
    "chat.send": "Send",
    "chat.relatedLabel": "Related questions",
    "chat.relatedAria": "Related questions",
    "chat.allSubjects": "All subjects",
    "chat.defaultSessionTitle": "Session without a question",
    "chat.sessionActions": "Session actions",
    "chat.starSession": "Star",
    "chat.unstarSession": "Unstar",
    "chat.renameSession": "Rename",
    "chat.deleteSession": "Delete",
    "chat.renamePrompt": "New session name",
    "chat.deleteConfirm": "Delete this chat session?",
    "chat.sessionActionError": "Could not update the chat session.",
    "chat.loading": "Give me a moment, checking the documents...",
    "chat.requestError": "Could not process the question.",
    "chat.connectionError": "Could not connect to the server. Check the app and try again.",
    "chat.suggestions": [
      "Which subjects have indexed documents?",
      "Summarize the uploaded documents.",
      "What can I ask from the document repository?",
      "Which document should I read first?"
    ]
  },
  vi: {
    "nav.documents": "Tài liệu",
    "nav.chat": "Hỏi đáp",
    "nav.logout": "Đăng xuất",
    "nav.login": "Đăng nhập",
    "nav.register": "Tạo tài khoản",
    "shell.portal": "Cổng học tập",
    "shell.chatbot": "Chatbot",
    "shell.courses": "Môn học",
    "shell.documents": "Kho tài liệu",
    "shell.users": "Người dùng",
    "shell.help": "Trợ giúp",
    "shell.search": "Tìm kiếm trong hệ thống...",
    "shell.notifications": "Thông báo",
    "shell.history": "Lịch sử",
    "shell.changePassword": "Đổi mật khẩu",
    "admin.totalUsers": "Tổng người dùng",
    "admin.usersShown": "hiển thị",
    "admin.admins": "QUẢN TRỊ VIÊN",
    "admin.fullSystemAccess": "Toàn quyền hệ thống",
    "admin.lecturers": "GIẢNG VIÊN",
    "admin.manageSubjects": "Quản lý môn được phân công",
    "admin.students": "SINH VIÊN",
    "admin.readChatAccess": "Quyền đọc và chat",
    "admin.subjects": "MÔN HỌC",
    "admin.subjectsAssigned": "đã phân công",
    "admin.directory": "Danh bạ",
    "admin.usersUnit": "người dùng",
    "admin.searchDirectory": "Tìm kiếm",
    "admin.searchDirectoryPlaceholder": "Tên, email, vai trò, môn học...",
    "admin.roleFilter": "Vai trò",
    "admin.allRoles": "Tất cả vai trò",
    "admin.filter": "Lọc",
    "admin.clear": "Xóa bộ lọc",
    "admin.noMatchingUsers": "Không có người dùng phù hợp.",
    "admin.noMatchingUsersHint": "Xóa bộ lọc hoặc tạo tài khoản mới từ bảng điều khiển.",
    "admin.thUser": "NGƯỜI DÙNG",
    "admin.thCreated": "NGÀY TẠO",
    "admin.thRole": "VAI TRÒ",
    "admin.thSubjects": "MÔN HỌC",
    "admin.unassignedSubject": "Chưa đăng ký môn",
    "admin.allIndexedDocs": "Tất cả tài liệu đã index",
    "admin.notApplicable": "Không áp dụng",
    "admin.delete": "Xóa",
    "admin.lecturerManagement": "Quản lý giảng viên",
    "admin.lecturerTableHeading": "Bảng giảng viên",
    "admin.lecturersUnit": "giảng viên",
    "admin.noLecturersFound": "Không tìm thấy giảng viên nào.",
    "admin.createLecturerHint": "Hãy tạo tài khoản giảng viên trước.",
    "admin.thName": "TÊN",
    "admin.subjectLeaderSuffix": " (Trưởng bộ môn)",
    "admin.teachingLecturerRole": "Giảng viên giảng dạy",
    "admin.leaderRole": "Trưởng bộ môn",
    "admin.noSubjectsAvailable": "Không có môn nào",
    "admin.selectSubjectAssign": "Chọn môn để đăng ký",
    "admin.assignButton": "Đăng ký",
    "admin.studentManagement": "Quản lý học sinh",
    "admin.studentTableHeading": "Bảng học sinh",
    "admin.studentsUnit": "học sinh",
    "admin.noStudentsFound": "Không tìm thấy học sinh nào.",
    "admin.createStudentHint": "Hãy tạo tài khoản học sinh trước.",
    "admin.roleStudent": "Học sinh",
    "admin.roleLecturer": "Giảng viên",
    "admin.roleAdmin": "Quản trị viên",
    "admin.createAccountHeading": "Thêm user",
    "admin.fullName": "Họ và tên",
    "admin.email": "Email",
    "admin.password": "Mật khẩu",
    "admin.role": "Vai trò",
    "admin.subjectsForLecturer": "Môn học phân công",
    "admin.createUserBtn": "Tạo người dùng",
    "documents.shellTitle": "Chi tiết môn học",
    "documents.backToCatalog": "Quay lại danh sách môn",
    "documents.eyebrow": "Course documents",
    "documents.defaultDescription": "Quản lý chương, file và link bài giảng của môn này.",
    "documents.documentsUnit": "tài liệu",
    "documents.chaptersUnit": "chương",
    "documents.editSubjectInfo": "Sửa thông tin môn",
    "documents.subjectCode": "Mã môn",
    "documents.subjectDesc": "Mô tả",
    "documents.deleteSubject": "Xóa môn",
    "documents.saveSubject": "Lưu môn",
    "documents.addChapter": "Thêm chương/mục",
    "documents.chapterTitle": "Tên chương/mục",
    "documents.chapterTitlePlaceholder": "VD: Chapter 1 / Muc 1.1",
    "documents.sortOrder": "Thứ tự",
    "documents.addChapterBtn": "Thêm chương",
    "documents.addDocument": "Thêm tài liệu",
    "documents.documentFile": "File tài liệu",
    "documents.orLectureUrl": "Hoặc URL bài giảng",
    "documents.indexDocumentBtn": "Index tài liệu",
    "documents.chaptersAndDocuments": "Chương và tài liệu",
    "documents.noChapters": "Môn này chưa có chương/mục.",
    "documents.noChaptersHint": "Thêm chương trước, sau đó upload tài liệu vào đúng mục.",
    "documents.updateBtn": "Cập nhật",
    "documents.deleteBtn": "Xóa",
    "documents.addFileTo": "Thêm file vào ",
    "documents.chapterLabel": "Chương/mục",
    "documents.emptyChapter": "Chưa có tài liệu trong chương/mục này.",
    "documents.previewBtn": "Xem",
    "documents.editBtn": "Sửa",
    "documents.reindexBtn": "Re-index",
    "documents.deleteDocBtn": "Xóa",
    "documents.indexedContent": "Nội dung đã index",
    "documents.previewTitle": "Tài liệu",
    "documents.loadingContent": "Đang tải nội dung đã index...",
    "documents.reindexConfirm": "Re-index tài liệu này?",
    "documents.deleteConfirm": "Xóa tài liệu này khỏi kho index? Thao tác này không thể hoàn tác.",
    "documents.deleteChapterConfirm": "Xóa chương/mục này? Thao tác này không thể hoàn tác.",
    "documents.deleteSubjectConfirm": "Xóa môn này và toàn bộ chương bên trong? Thao tác này không thể hoàn tác.",
    "documents.navCatalog": "Danh mục môn học",
    "documents.catalogEmptyDesc": "Danh sách các chương đã tạo",
    "documents.emptyCatalogTitle": "Chưa có danh mục môn học.",
    "documents.emptyCatalogHint": "Tạo môn học trước, sau đó thêm chương để upload tài liệu theo danh mục chuẩn.",
    "documents.createFirstChapterBtn": "Tạo chương đầu tiên",
    "documents.uploadTitle": "Tải tài liệu",
    "documents.docInfo": "Thông tin tài liệu",
    "documents.chooseFileBtn": "Chọn file",
    "documents.url": "URL trang bài giảng",
    "documents.enterUrl": "Nhập URL trang bài giảng",
    "admin.usersTitle": "Người dùng",
    "admin.navDirectory": "Bảng người dùng",
    "admin.navOnlineUsers": "Người dùng online",
    "admin.navLecturerTable": "Bảng giảng viên",
    "admin.navStudentTable": "Bảng học sinh",
    "admin.navCreateAccount": "Thêm tài khoản",
    "admin.navImportExcel": "Nhập từ Excel",
    "admin.navCreateSubject": "Thêm môn",
    "admin.navSubjectManagement": "Quản lý môn học",
    "admin.noneLeader": "Chưa có",
    "admin.subjectCode": "Mã môn",
    "admin.description": "Mô tả",
    "admin.optional": "Không bắt buộc",
    "admin.subjectList": "Danh sách môn học",
    "admin.subjectsUnit": "môn",
    "admin.noSubjects": "Chưa có môn học nào.",
    "admin.createSubjectPrompt": "Tạo môn học mới ở trên.",
    "admin.thSubject": "MÔN HỌC",
    "admin.thLeader": "TRƯỞNG MÔN",
    "admin.thStudents": "SINH VIÊN",
    "admin.thStatus": "TRẠNG THÁI",
    "admin.thActions": "THAO TÁC",
    "admin.active": "Đang mở",
    "admin.inactive": "Đã ẩn",
    "admin.deactivate": "Ẩn",
    "admin.activate": "Mở",
    "courses.eyebrow": "Không gian học tập",
    "courses.description": "Mở không gian học tập để xem tài liệu, chương/mục và trao đổi về môn học.",
    "courses.emptyTitle": "Chưa có không gian môn học nào.",
    "courses.emptyHint": "Vui lòng tải lên và index tài liệu môn học trước, sau đó quay lại đây.",
    "courses.noDescription": "Chưa có mô tả.",
    "courses.unassigned": "Chưa phân công",
    "courses.chaptersUnit": "Chương",
    "courseWorkspace.backToCourses": "Quay lại danh sách",
    "courseWorkspace.defaultDescription": "Không gian môn học chứa tài liệu, các chương và chat môn học.",
    "courseWorkspace.chatCourse": "Chat môn này",
    "courseWorkspace.manageDocuments": "Quản lý tài liệu",
    "courseWorkspace.documents": "TÀI LIỆU",
    "courseWorkspace.chapters": "CHƯƠNG",
    "courseWorkspace.lecturer": "GIẢNG VIÊN",
    "courseWorkspace.noEmail": "Chưa có email",
    "courseWorkspace.courseMap": "Bản đồ môn học",
    "courseWorkspace.chaptersAndDocs": "Chương và tài liệu",
    "courseWorkspace.emptyChaptersTitle": "Chưa có chương nào được tải nội dung lên.",
    "courseWorkspace.emptyChaptersHint": "Tải tài liệu lên hoặc nhờ giảng viên bổ sung nội dung.",
    "courseWorkspace.docsUnit": "tài liệu",
    "courseWorkspace.chunksUnit": "chunks",
    "admin.importExcelEyebrow": "Excel",
    "admin.importExcelTitle": "Nhập từ Excel",
    "admin.excelFile": "File Excel",
    "admin.excelImportHint": "Hệ thống tự động trích xuất cột Họ & Tên và Email.",
    "admin.defaultRole": "Vai trò mặc định",
    "admin.defaultLecturerSubjects": "Môn mặc định cho giảng viên",
    "admin.noUnassignedSubjects": "Không còn môn chưa gán",
    "admin.importUsers": "Nhập người dùng",
    "admin.importWelcomeNote": "Email chào mừng sẽ có vai trò và quyền truy cập môn học.",
    "admin.confirmImportUsers": "Tạo người dùng hàng loạt và gửi email chào mừng?",
    "admin.confirmRoleChange": "Đổi vai trò của {user} sang {role}?",
    "admin.roleChangeTitle": "Đổi vai trò sẽ hỏi xác nhận và tự lưu.",
    "onlineUsers.title": "Người dùng online",
    "onlineUsers.connecting": "Đang kết nối...",
    "onlineUsers.loading": "Đang tải người dùng online...",
    "onlineUsers.empty": "Hiện không có người dùng nào hoạt động.",
    "onlineUsers.activeNow": "Đang hoạt động",
    "onlineUsers.realtimeNote": "Trạng thái realtime",
    "onlineUsers.statusHelp": "Danh sách tự cập nhật khi người dùng vào hoặc rời hệ thống.",
    "onlineUsers.summaryOne": "1 người dùng online (đang hoạt động)",
    "onlineUsers.summaryMany": "{count} người dùng online (đang hoạt động)",
    "onlineUsers.youAreOnline": "Bạn đang online",
    "onlineUsers.online": "Online",
    "onlineUsers.tabs": "{count} tab",
    "onlineUsers.expand": "Mở danh sách người dùng online",
    "onlineUsers.collapse": "Thu gọn danh sách người dùng online",
    "documents.shellTitle": "Kho tài liệu",
    "documents.manageTitle": "Quản lý kho tài liệu",
    "documents.manageSubtitle": "Upload tài liệu học tập, trích xuất nội dung và index để chatbot trả lời có nguồn.",
    "documents.uploadKicker": "Upload",
    "documents.dropTitle": "Kéo thả tài liệu vào đây",
    "documents.chooseFile": "Chọn tệp hoặc kéo thả vào đây",
    "documents.urlRenderHint": "Trang SPA/React/Vue sẽ được render trước khi trích xuất DOM.",
    "documents.storage": "Lưu trữ",
    "documents.uploadedSize": "Dung lượng đã upload",
    "documents.totalUploaded": "Tổng đã upload",
    "documents.documentList": "Danh sách tài liệu",
    "documents.showing": "Hiển thị",
    "documents.fileName": "Tên file",
    "documents.status": "Trạng thái",
    "documents.uploadDate": "Ngày upload",
    "documents.indexedStatus": "Đã Index",
    "documents.emptyTitle": "Chưa có tài liệu.",
    "documents.emptyHint": "Upload giáo trình hoặc URL bài giảng để bắt đầu hỏi đáp theo nguồn.",
    "assistant.open": "Mở trang chat",
    "assistant.hidden": "Chào bạn, mình là chatbot hỗ trợ tìm kiếm thông tin nhanh hơn.",
    "documents.title": "Kho tài liệu môn học",
    "documents.subtitle": "Quản lý tài liệu đã upload, trích xuất nội dung và lập chỉ mục cho trang hỏi đáp riêng.",
    "documents.openChat": "Mở trang chat",
    "documents.statsAria": "Thống kê kho tài liệu",
    "documents.statsDocuments": "Tài liệu",
    "documents.statsIndexed": "Đã index",
    "documents.statsProcessing": "Đang xử lý",
    "documents.uploadTitle": "Upload tài liệu",
    "documents.uploadSubtitle": "PDF, DOCX, PPTX, TXT hoặc URL trang bài giảng sẽ được trích xuất, chunk và embed tự động.",
    "documents.subject": "Môn học",
    "documents.chapter": "Chương",
    "documents.source": "Nguồn",
    "documents.subjectPlaceholder": "VD: IOT102",
    "documents.chapterPlaceholder": "VD: Chương 1 hoặc Tuần 1",
    "documents.dropzoneTitle": "Kéo thả tài liệu hoặc bấm để chọn file",
    "documents.dropzoneDefault": "Hỗ trợ PDF, DOCX, PPTX, TXT",
    "documents.or": "hoặc",
    "documents.url": "URL trang bài giảng",
    "documents.urlPlaceholder": "https://example.com/bai-giang-react-vue",
    "documents.urlHint": "Trang SPA/React/Vue sẽ được render bằng Playwright trước khi trích xuất DOM.",
    "documents.submit": "Tải lên và index",
    "documents.indexedTitle": "Tài liệu đã lập chỉ mục",
    "documents.filesUnit": "file",
    "documents.empty": "Chưa có tài liệu nào. Hãy tải tài liệu môn học để bắt đầu hỏi đáp theo nguồn.",
    "documents.view": "Xem",
    "documents.navList": "Danh s\u00e1ch t\u00e0i li\u1ec7u",
    "documents.navUpload": "Upload & index",
    "documents.navUpload": "Upload & index",
    "documents.navCatalog": "Danh m\u1ee5c m\u00f4n h\u1ecdc",
    "documents.eyebrow": "Quản lý nội dung học tập",
    "documents.kpiTotal": "Tổng cộng",
    "documents.kpiFailed": "Thất bại",
    "documents.selectSubject": "Chọn môn học",
    "documents.noSubjectAssigned": "Chưa có môn nào được gán cho tài khoản này.",
    "documents.reindexStale": "Re-index tài liệu cũ",
    "documents.search": "Tìm kiếm",
    "documents.searchPlaceholder": "Tên file, môn học, chương...",
    "documents.allSubjects": "Tất cả môn học",
    "documents.allStatus": "Tất cả trạng thái",
    "documents.filter": "Lọc",
    "documents.clear": "Xóa bộ lọc",
    "documents.uploader": "Người tải lên",
    "documents.subjectChapter": "Môn học / Chương",
    "documents.chunksSize": "Số chunk / Kích thước",
    "documents.chunksUnit": "chunk",
    "documents.unknown": "Không rõ",
    "documents.actions": "Thao tác",
    "documents.edit": "Sửa",
    "documents.reindex": "Re-index",
    "documents.delete": "Xóa",
    "chat.sessionsAria": "Lịch sử phiên chat",
    "chat.documents": "Kho tài liệu",
    "chat.newSession": "Phiên mới",
    "chat.history": "Lịch sử phiên",
    "chat.sessionsUnit": "phiên",
    "chat.messagesUnit": "tin",
    "chat.noSessions": "Chưa có phiên nào.",
    "chat.mainAria": "Chat theo tài liệu",
    "chat.title": "Chat tài liệu",
    "chat.subtitle": "Hỏi theo kho tài liệu đã index. Câu hỏi ngoài phạm vi tài liệu sẽ được báo không đủ dữ liệu.",
    "chat.headerKicker": "Trợ lý môn học",
    "chat.headerTitle": "Nhập môn AI",
    "chat.headerSubtitle": "Hỏi đáp dựa trên tài liệu đã index. Nếu dữ liệu không đủ, chatbot phải báo thiếu nguồn thay vì đoán.",
    "chat.currentSession": "Phiên hiện tại",
    "chat.emptyTitle": "Bắt đầu bằng một câu hỏi cụ thể",
    "chat.emptyText": "Chọn gợi ý bên dưới hoặc nhập câu hỏi của bạn. Nếu tài liệu không đủ dữ liệu, chatbot sẽ báo rõ thay vì đoán.",
    "chat.suggestionsAria": "Gợi ý câu hỏi",
    "chat.welcome": "Chào bạn, hỏi mình về tài liệu đã upload nhé. Có đủ dữ liệu thì mình trả lời gọn và kèm nguồn.",
    "chat.placeholder": "Hỏi về môn, chương hoặc tài liệu đã index...",
    "chat.send": "Gửi",
    "chat.relatedLabel": "Câu hỏi liên quan",
    "chat.relatedAria": "Câu hỏi liên quan",
    "chat.allSubjects": "Tất cả môn",
    "chat.defaultSessionTitle": "Phiên chưa có câu hỏi",
    "chat.sessionActions": "Thao tác phiên",
    "chat.starSession": "Ghim",
    "chat.unstarSession": "Bỏ ghim",
    "chat.renameSession": "Đổi tên",
    "chat.deleteSession": "Xóa",
    "chat.renamePrompt": "Tên phiên mới",
    "chat.deleteConfirm": "Xóa phiên chat này?",
    "chat.sessionActionError": "Không cập nhật được phiên chat.",
    "chat.loading": "Chờ mình chút, đang dò trong tài liệu...",
    "chat.requestError": "Không xử lý được câu hỏi.",
    "chat.connectionError": "Không kết nối được server. Kiểm tra lại ứng dụng rồi thử tiếp.",
    "chat.suggestions": [
      "Hiện có những môn nào đã index tài liệu?",
      "Tóm tắt các tài liệu đã upload.",
      "Tôi có thể hỏi gì từ kho tài liệu?",
      "Nên đọc tài liệu nào trước?"
    ]
  }
};

const languageKey = "courseAssistantLanguage";
const chatPage = document.querySelector(".rbl-chat-page");
const documentsPage = document.querySelector(".ops-documents-page");
const chatForm = document.getElementById("chatForm");
const questionInput = document.getElementById("questionInput");
const chatMessages = document.getElementById("chatMessages");
const newSessionButton = document.getElementById("newSessionButton");
const chatSessionList = document.getElementById("chatSessionList");
const activeSessionTitle = document.getElementById("activeSessionTitle");
const documentDropzone = document.getElementById("documentDropzone");
const documentFileInput = document.getElementById("documentFileInput");
const documentFileName = document.getElementById("documentFileName");
const documentPreviewModal = document.getElementById("documentPreviewModal");
const documentPreviewTitle = document.getElementById("documentPreviewTitle");
const documentPreviewMeta = document.getElementById("documentPreviewMeta");
const documentPreviewBody = document.getElementById("documentPreviewBody");
const assistantLauncher = document.getElementById("chatbotHelper");
const assistantLauncherButton = document.getElementById("chatbotHelperButton");
const onlineUsersWidget = document.getElementById("onlineUsersWidget");
const onlineUsersList = document.getElementById("onlineUsersList");
const onlineUsersSummary = document.querySelector("[data-online-users-summary]");
const onlineUsersToggle = document.querySelector("[data-online-users-toggle]");
const onlineUsersWidgetStateKey = "courseAssistantOnlineUsersWidgetCollapsed";
let isSending = false;
let latestOnlineUsersSnapshot = null;
const subjectQuestionSubjects = readSubjectQuestionSubjects();
const relatedQuestionPool = readRelatedQuestionPool();

function getLanguage() {
  return localStorage.getItem(languageKey) === "en" ? "en" : "vi";
}

function t(key) {
  return translations[getLanguage()][key] || translations.en[key] || key;
}

function readJsonDataAttribute(element, key, fallback) {
  if (!element?.dataset?.[key]) {
    return fallback;
  }

  try {
    return JSON.parse(element.dataset[key]);
  } catch {
    return fallback;
  }
}

function buildDefaultSuggestionItems() {
  const enSuggestions = readJsonDataAttribute(chatPage, "chatSuggestionsEn", translations.en["chat.suggestions"]);
  const viSuggestions = readJsonDataAttribute(chatPage, "chatSuggestionsVi", translations.vi["chat.suggestions"]);
  const total = Math.max(enSuggestions.length, viSuggestions.length);

  return Array.from({ length: total }, (_, index) => ({
    id: `default-${index}`,
    en: enSuggestions[index] || viSuggestions[index] || "",
    vi: viSuggestions[index] || enSuggestions[index] || ""
  })).filter((item) => item.en || item.vi);
}

function getChatSuggestionItems() {
  const selectedSubject = getSelectedSubjectFilter();
  if (selectedSubject) {
    return buildSubjectQuestionItems(selectedSubject);
  }

  return dedupeQuestionItems([
    ...buildAllSubjectQuestionItems(),
    ...relatedQuestionPool,
    ...buildDefaultSuggestionItems()
  ]);
}

function getChatSuggestions(language = getLanguage()) {
  const asked = readAskedQuestions();
  const basePool = getChatSuggestionItems();
  return getAvailableQuestionItems(basePool, asked, getSelectedSubjectFilter())
    .slice(0, 6)
    .map((item) => language === "vi" ? item.vi : item.en)
    .filter(Boolean);
}

function readSubjectQuestionSubjects() {
  const fromPayload = readJsonDataAttribute(chatPage, "chatSubjectSuggestions", [])
    .map((item) => item?.subject || item?.vi || item?.en || "")
    .filter(Boolean);
  const fromChips = [...document.querySelectorAll(".chat-subject-chip")]
    .map((button) => button.dataset.subjectFilter || "")
    .filter(Boolean);

  return [...new Set([...fromPayload, ...fromChips].map((subject) => subject.trim()).filter(Boolean))];
}

function dedupeQuestionItems(items) {
  const seen = new Set();
  const result = [];
  for (const item of items) {
    if (!item?.en || !item?.vi) {
      continue;
    }

    const key = `${normalizeQuestionForMemory(item.en)}|${normalizeQuestionForMemory(item.vi)}`;
    if (seen.has(key)) {
      continue;
    }

    seen.add(key);
    result.push(item);
  }

  return result;
}

function getAvailableQuestionItems(basePool, asked, selectedSubject = "") {
  const available = dedupeQuestionItems(basePool).filter((item) => !questionWasAsked(item, asked));
  if (available.length > 0) {
    return available;
  }

  const recoveryPool = selectedSubject
    ? buildRecoveryQuestionItems(selectedSubject)
    : subjectQuestionSubjects.flatMap((subject) => buildRecoveryQuestionItems(subject));
  return dedupeQuestionItems(recoveryPool).filter((item) => !questionWasAsked(item, asked));
}

function readRelatedQuestionPool() {
  const items = readJsonDataAttribute(chatPage, "chatRelatedQuestions", []);
  if (Array.isArray(items) && items.length > 0) {
    return items
      .filter((item) => item?.en && item?.vi)
      .map((item, index) => ({
        id: item.id || `subject-${index}`,
        subject: item.subject || "",
        en: item.en,
        vi: item.vi
      }));
  }

  return [
    {
      id: "available-subjects",
      subject: "",
      en: "Which subjects have indexed documents?",
      vi: "Hiện có những môn nào đã index tài liệu?"
    },
    {
      id: "summarize-documents",
      subject: "",
      en: "Summarize the uploaded documents.",
      vi: "Tóm tắt các tài liệu đã upload."
    },
    {
      id: "askable-content",
      subject: "",
      en: "What can I ask from the document repository?",
      vi: "Tôi có thể hỏi gì từ kho tài liệu?"
    }
  ];
}

async function ensureVietnameseFontReady() {
  if (!document.fonts?.load) {
    return;
  }

  await Promise.all([
    document.fonts.load('400 16px "Be Vietnam Pro"'),
    document.fonts.load('600 16px "Be Vietnam Pro"'),
    document.fonts.load('800 16px "Be Vietnam Pro"')
  ]);
}

async function setLanguage(language) {
  const nextLanguage = language === "vi" ? "vi" : "en";
  if (getLanguage() === nextLanguage) {
    return;
  }

  try {
    if (nextLanguage === "vi") {
      await ensureVietnameseFontReady();
    }

    localStorage.setItem(languageKey, nextLanguage);
    document.documentElement.classList.add("is-language-changing");
    applyLanguage();
  } finally {
    window.requestAnimationFrame(() => {
      window.requestAnimationFrame(() => {
        document.documentElement.classList.remove("is-language-changing");
      });
    });
  }
}

function applyLanguage() {
  const language = getLanguage();
  document.documentElement.lang = language === "vi" ? "vi" : "en";

  document.querySelectorAll("[data-i18n]").forEach((element) => {
    element.textContent = translations[language][element.dataset.i18n] || translations.en[element.dataset.i18n] || element.textContent;
  });

  document.querySelectorAll("[data-i18n-placeholder]").forEach((element) => {
    element.placeholder = translations[language][element.dataset.i18nPlaceholder] || translations.en[element.dataset.i18nPlaceholder] || element.placeholder;
  });

  document.querySelectorAll("[data-i18n-aria-label]").forEach((element) => {
    element.setAttribute("aria-label", translations[language][element.dataset.i18nAriaLabel] || translations.en[element.dataset.i18nAriaLabel] || element.getAttribute("aria-label"));
  });

  document.querySelectorAll("[data-i18n-title]").forEach((element) => {
    element.setAttribute("title", translations[language][element.dataset.i18nTitle] || translations.en[element.dataset.i18nTitle] || element.getAttribute("title"));
  });

  document.querySelectorAll("[data-language-option]").forEach((button) => {
    const isActive = button.dataset.languageOption === language;
    button.classList.toggle("is-active", isActive);
    button.setAttribute("aria-pressed", String(isActive));
  });

  document.querySelectorAll("[data-language-field]").forEach((field) => {
    field.value = language;
  });

  document.querySelectorAll("[data-assistant-greeting]").forEach((element) => {
    const name = element.dataset.assistantName || "you";
    element.textContent = language === "vi"
      ? `Chào ${name}, mình sẽ hỗ trợ bạn tìm kiếm thông tin nhanh hơn.`
      : `Hi ${name}, I can help you find information faster.`;
  });

  updateSuggestionButtons();
  updateRelatedQuestionButtons();
  updateDropzoneDefaultText();
  if (latestOnlineUsersSnapshot) {
    renderOnlineUsersSnapshot(latestOnlineUsersSnapshot);
  }
  document.documentElement.classList.remove("i18n-pending");
  document.documentElement.classList.add("i18n-ready");
}

function updateSuggestionButtons() {
  const language = getLanguage();
  const suggestions = getChatSuggestions(language);
  document.querySelectorAll(".suggestion-chip").forEach((button, index) => {
    const question = suggestions[index];
    if (!question) {
      button.hidden = true;
      button.dataset.question = "";
      return;
    }

    button.hidden = false;
    button.textContent = question;
    button.dataset.question = question;
  });
}

function updateRelatedQuestionButtons() {
  renderRelatedQuestions();
}

function getSelectedSubjectFilter() {
  return document.querySelector(".chat-subject-chip.is-active")?.dataset.subjectFilter || "";
}

function setSelectedSubjectFilter(subject) {
  const normalizedSubject = subject || "";
  document.querySelectorAll(".chat-subject-chip").forEach((button) => {
    const isActive = (button.dataset.subjectFilter || "") === normalizedSubject;
    button.classList.toggle("is-active", isActive);
    button.setAttribute("aria-pressed", String(isActive));
  });
  updateSuggestionButtons();
  renderRelatedQuestions();
}

function bindSubjectFilterChips() {
  document.querySelectorAll(".chat-subject-chip").forEach((button) => {
    button.addEventListener("click", () => {
      setSelectedSubjectFilter(button.dataset.subjectFilter || "");
      questionInput?.focus();
    });
  });
}

function bindSubjectSuggestionButtons() {
  document.querySelectorAll("[data-subject-suggestion]").forEach((button) => {
    if (button.dataset.subjectSuggestionBound === "true") {
      return;
    }

    button.dataset.subjectSuggestionBound = "true";
    button.addEventListener("click", () => {
      setSelectedSubjectFilter(button.dataset.subjectSuggestion || "");
      questionInput?.focus();
    });
  });
}

function applyInitialSubjectFromUrl() {
  if (!chatPage) {
    return;
  }

  const subject = new URLSearchParams(window.location.search).get("subject") || "";
  if (!subject.trim()) {
    return;
  }

  const matchingChip = [...document.querySelectorAll(".chat-subject-chip")]
    .find((button) => (button.dataset.subjectFilter || "").toLowerCase() === subject.trim().toLowerCase());
  if (matchingChip) {
    setSelectedSubjectFilter(matchingChip.dataset.subjectFilter || "");
  }
}

function normalizeQuestionForMemory(question) {
  return (question || "")
    .normalize("NFD")
    .replace(/\p{M}/gu, "")
    .toLowerCase()
    .replace(/[^\p{L}\p{N}]+/gu, " ")
    .trim();
}

function getAskedQuestionKey() {
  return `ragChatAskedQuestions:${getSessionId()}`;
}

function readAskedQuestions() {
  try {
    return new Set(JSON.parse(localStorage.getItem(getAskedQuestionKey()) || "[]"));
  } catch {
    return new Set();
  }
}

function rememberAskedQuestion(question) {
  const normalized = normalizeQuestionForMemory(question);
  if (!normalized) {
    return;
  }

  const asked = readAskedQuestions();
  asked.add(normalized);
  localStorage.setItem(getAskedQuestionKey(), JSON.stringify([...asked].slice(-80)));
}

function collectVisibleUserQuestions() {
  document.querySelectorAll(".message.user .bubble").forEach((bubble) => {
    rememberAskedQuestion(bubble.textContent || "");
  });
}

function getRelatedRotationIndex() {
  return Number(sessionStorage.getItem(`ragChatRelatedRotation:${getSessionId()}`) || "0");
}

function advanceRelatedRotation() {
  const key = `ragChatRelatedRotation:${getSessionId()}`;
  sessionStorage.setItem(key, String(getRelatedRotationIndex() + 1));
}

function questionWasAsked(item, asked) {
  return asked.has(normalizeQuestionForMemory(item.en))
    || asked.has(normalizeQuestionForMemory(item.vi));
}

function normalizeSubjectForCompare(subject) {
  return normalizeQuestionForMemory(subject);
}

function extractCourseCode(value) {
  const match = String(value || "").match(/\b[A-Za-z]{2,}\d{2,}\b/);
  return match ? match[0].toUpperCase() : "";
}

function questionItemMatchesSubject(item, subject) {
  const selectedCode = extractCourseCode(subject);
  const itemCode = extractCourseCode(item?.subject || `${item?.en || ""} ${item?.vi || ""}`);
  if (selectedCode && itemCode) {
    return selectedCode === itemCode;
  }

  const normalizedSubject = normalizeSubjectForCompare(subject);
  const normalizedItemSubject = normalizeSubjectForCompare(item?.subject || "");
  return normalizedSubject
    && normalizedItemSubject
    && (normalizedItemSubject === normalizedSubject
      || normalizedItemSubject.includes(normalizedSubject)
      || normalizedSubject.includes(normalizedItemSubject));
}

function getKnownQuestionItemsForSubject(subject) {
  return relatedQuestionPool.filter((item) => questionItemMatchesSubject(item, subject));
}

function buildSubjectQuestionItems(subject) {
  const trimmedSubject = (subject || "").trim();
  if (!trimmedSubject) {
    return [];
  }

  const knownItems = getKnownQuestionItemsForSubject(trimmedSubject);
  if (knownItems.length > 0) {
    return knownItems;
  }

  return [
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-credits`,
      en: `How many credits does ${trimmedSubject} have?`,
      vi: `${trimmedSubject} có bao nhiêu tín chỉ?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-about`,
      en: `What is ${trimmedSubject} about?`,
      vi: `${trimmedSubject} là môn gì?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-contents`,
      en: `What are the main contents of ${trimmedSubject}?`,
      vi: `Nội dung chính của ${trimmedSubject} gồm những gì?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-assessment`,
      en: `How is ${trimmedSubject} assessed?`,
      vi: `${trimmedSubject} được đánh giá như thế nào?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-outcomes`,
      en: `What learning outcomes does ${trimmedSubject} mention?`,
      vi: `${trimmedSubject} có chuẩn đầu ra nào?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-materials`,
      en: `What materials or resources are used in ${trimmedSubject}?`,
      vi: `${trimmedSubject} dùng tài liệu hoặc nguồn học nào?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-student-tasks`,
      en: `What does the syllabus say students need to do in ${trimmedSubject}?`,
      vi: `Sinh viên cần làm gì trong ${trimmedSubject}?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-exams`,
      en: `What exam or assessment percentages are listed for ${trimmedSubject}?`,
      vi: `${trimmedSubject} có tỷ lệ thi hoặc đánh giá nào?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-chapters`,
      en: `Which chapters or sections are indexed for ${trimmedSubject}?`,
      vi: `${trimmedSubject} đã index những chương hoặc phần nào?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-objectives`,
      en: `What are the objectives of ${trimmedSubject}?`,
      vi: `Mục tiêu của ${trimmedSubject} là gì?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-prerequisites`,
      en: `Does ${trimmedSubject} mention any prerequisites?`,
      vi: `${trimmedSubject} có yêu cầu tiên quyết nào không?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-schedule`,
      en: `What study schedule or weekly plan is listed for ${trimmedSubject}?`,
      vi: `${trimmedSubject} có lịch học hoặc kế hoạch tuần nào?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-activities`,
      en: `What learning activities are mentioned in ${trimmedSubject}?`,
      vi: `${trimmedSubject} có những hoạt động học tập nào?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-tools`,
      en: `What tools or platforms are used in ${trimmedSubject}?`,
      vi: `${trimmedSubject} sử dụng công cụ hoặc nền tảng nào?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-completion`,
      en: `What completion criteria are listed for ${trimmedSubject}?`,
      vi: `${trimmedSubject} có tiêu chí hoàn thành nào?`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-summary`,
      en: `Summarize the indexed syllabus for ${trimmedSubject}.`,
      vi: `Tóm tắt syllabus đã index của ${trimmedSubject}.`
    },
    {
      id: `${normalizeSubjectForCompare(trimmedSubject)}-important-notes`,
      en: `What important notes should students remember for ${trimmedSubject}?`,
      vi: `Sinh viên cần lưu ý gì khi học ${trimmedSubject}?`
    }
  ];
}

function buildAllSubjectQuestionItems() {
  return subjectQuestionSubjects.flatMap((subject) => buildSubjectQuestionItems(subject));
}

function buildRecoveryQuestionItems(subject) {
  const trimmedSubject = (subject || "").trim();
  if (!trimmedSubject) {
    return [];
  }

  const subjectKey = normalizeSubjectForCompare(trimmedSubject);
  return [
    {
      id: `${subjectKey}-recovery-teacher-expectations`,
      en: `What does the lecturer expect students to prepare for ${trimmedSubject}?`,
      vi: `Giảng viên yêu cầu sinh viên chuẩn bị gì cho ${trimmedSubject}?`
    },
    {
      id: `${subjectKey}-recovery-output-products`,
      en: `What products, assignments, or submissions are required in ${trimmedSubject}?`,
      vi: `${trimmedSubject} yêu cầu bài tập, sản phẩm hoặc bài nộp nào?`
    },
    {
      id: `${subjectKey}-recovery-study-resources`,
      en: `Which links, files, or learning resources are mentioned for ${trimmedSubject}?`,
      vi: `${trimmedSubject} có link, file hoặc nguồn học nào được nhắc đến?`
    },
    {
      id: `${subjectKey}-recovery-grading-guide`,
      en: `What grading guide or rubrics are mentioned for ${trimmedSubject}?`,
      vi: `${trimmedSubject} có hướng dẫn chấm điểm hoặc rubric nào?`
    },
    {
      id: `${subjectKey}-recovery-first-read`,
      en: `What should I read first in the indexed material for ${trimmedSubject}?`,
      vi: `Nên đọc phần nào trước trong tài liệu đã index của ${trimmedSubject}?`
    }
  ];
}

function updateRelatedQuestionsLabel(selectedSubject) {
  const label = document.querySelector(".chat-related-strip > span");
  if (!label) {
    return;
  }

  label.textContent = selectedSubject
    ? t("chat.relatedLabel")
    : (getLanguage() === "vi" ? "Câu hỏi gợi ý" : "Suggested questions");
}

function renderRelatedQuestions() {
  const list = document.querySelector(".chat-related-list");
  if (!list) {
    return;
  }

  const strip = list.closest(".chat-related-strip");
  collectVisibleUserQuestions();
  const language = getLanguage();
  const selectedSubject = getSelectedSubjectFilter();
  updateRelatedQuestionsLabel(selectedSubject);

  const asked = readAskedQuestions();
  const basePool = selectedSubject
    ? buildSubjectQuestionItems(selectedSubject)
    : dedupeQuestionItems([
        ...buildAllSubjectQuestionItems(),
        ...relatedQuestionPool,
        ...buildDefaultSuggestionItems()
      ]);
  const pool = getAvailableQuestionItems(basePool, asked, selectedSubject);
  if (pool.length === 0) {
    list.innerHTML = "";
    if (strip) {
      strip.hidden = true;
    }
    return;
  }

  if (strip) {
    strip.hidden = false;
  }
  const offset = getRelatedRotationIndex() % pool.length;
  const ordered = [...pool.slice(offset), ...pool.slice(0, offset)];
  const currentQuestions = new Set(
    [...list.querySelectorAll(".related-question-chip")]
      .map((button) => normalizeQuestionForMemory(button.dataset.question || button.textContent))
      .filter(Boolean));
  const picked = [];

  for (const item of ordered) {
    const text = language === "vi" ? item.vi : item.en;
    const normalized = normalizeQuestionForMemory(text);
    if (!normalized || picked.some((pickedItem) => pickedItem.id === item.id)) {
      continue;
    }

    if (pool.length > 8 && currentQuestions.has(normalized)) {
      continue;
    }

    picked.push(item);
    if (picked.length === 8) {
      break;
    }
  }

  if (picked.length < 8) {
    for (const item of ordered) {
      if (!picked.some((pickedItem) => pickedItem.id === item.id)) {
        picked.push(item);
      }
      if (picked.length === 8) {
        break;
      }
    }
  }

  list.innerHTML = picked.map((item) => {
    const text = language === "vi" ? item.vi : item.en;
    return `<button type="button" class="related-question-chip" data-question-id="${escapeHtml(item.id)}" data-question-subject="${escapeHtml(item.subject || "")}" data-question="${escapeHtml(text)}" data-question-en="${escapeHtml(item.en)}" data-question-vi="${escapeHtml(item.vi)}">${escapeHtml(text)}</button>`;
  }).join("");
  bindSuggestionButtons();
}

function updateDropzoneDefaultText() {
  if (!documentFileInput || !documentFileName || documentFileInput.files.length > 0) {
    return;
  }

  documentFileName.textContent = t("documents.dropzoneDefault");
}

function createSessionId() {
  if (crypto.randomUUID) {
    return crypto.randomUUID();
  }

  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (character) => {
    const random = Math.random() * 16 | 0;
    const value = character === "x" ? random : (random & 0x3 | 0x8);
    return value.toString(16);
  });
}

function getSessionId() {
  let sessionId = localStorage.getItem("ragChatSessionId");
  if (!sessionId) {
    sessionId = createSessionId();
    localStorage.setItem("ragChatSessionId", sessionId);
  }

  return sessionId;
}

function setSessionId(sessionId) {
  localStorage.setItem("ragChatSessionId", sessionId);
  markActiveSession(sessionId);
  updateSuggestionButtons();
  renderRelatedQuestions();
}

function formatSessionTime(value) {
  if (!value) {
    return "";
  }

  return new Date(value).toLocaleString(getLanguage() === "vi" ? "vi-VN" : "en-US", {
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit"
  });
}

function getSessionTitle(session) {
  return session?.title || t("chat.defaultSessionTitle");
}

function renderWelcomeMessage() {
  if (!chatMessages) {
    return;
  }

  const suggestions = getChatSuggestions();
  chatMessages.innerHTML = `
    <div class="chat-empty-state">
      <span class="empty-state-mark">AI</span>
      <h3 data-i18n="chat.emptyTitle">${escapeHtml(t("chat.emptyTitle"))}</h3>
      <p data-i18n="chat.emptyText">${escapeHtml(t("chat.emptyText"))}</p>
      <div class="suggestion-grid" aria-label="${escapeHtml(t("chat.suggestionsAria"))}" data-i18n-aria-label="chat.suggestionsAria">
        ${suggestions.map((question) => `<button type="button" class="suggestion-chip" data-question="${escapeHtml(question)}">${escapeHtml(question)}</button>`).join("")}
      </div>
    </div>
    <div class="message assistant">
      <div class="bubble" data-i18n="chat.welcome">${escapeHtml(t("chat.welcome"))}</div>
    </div>`;
  bindSuggestionButtons();
  applyLanguage();
}

function renderSessionMessages(messages) {
  if (!chatMessages) {
    return;
  }

  chatMessages.innerHTML = "";
  if (!messages || messages.length === 0) {
    renderWelcomeMessage();
    return;
  }

  messages.forEach((message) => {
    appendMessageTo(chatMessages, message.role, message.content, message.citations || []);
  });
  renderRelatedQuestions();
}

function markActiveSession(sessionId) {
  document.querySelectorAll(".chat-session-item").forEach((button) => {
    button.classList.toggle("is-active", button.dataset.sessionId === sessionId);
  });
}

function closeSessionMenus(exceptMenu = null) {
  document.querySelectorAll(".chat-session-menu.is-open").forEach((menu) => {
    if (menu !== exceptMenu) {
      menu.classList.remove("is-open");
    }
  });
}

function renderSessionList(sessions) {
  if (!chatSessionList) {
    return;
  }

  const sessionCount = document.getElementById("sessionCount");
  if (sessionCount) {
    sessionCount.textContent = sessions?.length ?? 0;
  }

  if (!sessions || sessions.length === 0) {
    chatSessionList.innerHTML = `<p class="session-empty" data-i18n="chat.noSessions">${escapeHtml(t("chat.noSessions"))}</p>`;
    applyLanguage();
    return;
  }

  chatSessionList.innerHTML = sessions.map((session) => `
    <div class="chat-session-entry${session.isStarred ? " is-starred" : ""}" data-session-entry data-session-id="${session.id}">
      <button type="button" class="chat-session-item" data-session-id="${session.id}">
        <span>${session.isStarred ? `<span class="material-symbols-outlined session-star" aria-hidden="true">star</span>` : ""}${escapeHtml(getSessionTitle(session))}</span>
        <small>${formatSessionTime(session.updatedAt)} / ${session.messageCount ?? 0} ${escapeHtml(t("chat.messagesUnit"))}</small>
      </button>
      <button type="button" class="chat-session-menu-button" data-session-menu-toggle data-session-id="${session.id}" aria-label="${escapeHtml(t("chat.sessionActions"))}">
        <span class="material-symbols-outlined" aria-hidden="true">more_vert</span>
      </button>
      <div class="chat-session-menu" data-session-menu>
        <button type="button" data-session-action="star" data-session-id="${session.id}" data-session-starred="${session.isStarred ? "true" : "false"}">
          <span class="material-symbols-outlined" aria-hidden="true">star</span>
          <span>${escapeHtml(t(session.isStarred ? "chat.unstarSession" : "chat.starSession"))}</span>
        </button>
        <button type="button" data-session-action="rename" data-session-id="${session.id}">
          <span class="material-symbols-outlined" aria-hidden="true">edit</span>
          <span>${escapeHtml(t("chat.renameSession"))}</span>
        </button>
        <button type="button" class="danger" data-session-action="delete" data-session-id="${session.id}">
          <span class="material-symbols-outlined" aria-hidden="true">delete</span>
          <span>${escapeHtml(t("chat.deleteSession"))}</span>
        </button>
      </div>
    </div>
  `).join("");
  bindSessionButtons();
  markActiveSession(getSessionId());
}

function escapeHtml(value) {
  const div = document.createElement("div");
  div.textContent = value || "";
  return div.innerHTML;
}


function setOnlineUsersConnectionState(state) {
  if (!onlineUsersWidget) {
    return;
  }

  onlineUsersWidget.dataset.connectionState = state || "disconnected";
}

function normalizeOnlineUsersPayload(payload) {
  if (!payload || typeof payload !== "object") {
    return null;
  }

  const users = Array.isArray(payload.users)
    ? payload.users.map((item) => ({
        userKey: item.userKey || "",
        userId: item.userId || "",
        displayName: item.displayName || item.email || "Unknown user",
        email: item.email || "",
        role: item.role || "",
        initials: item.initials || "U",
        connectionCount: Number(item.connectionCount || 0)
      }))
    : [];

  return {
    onlineUserCount: Number(payload.onlineUserCount || users.length || 0),
    users,
    windowLabel: payload.windowLabel || "active now"
  };
}

function formatOnlineUsersSummary(count) {
  const key = count === 1 ? "onlineUsers.summaryOne" : "onlineUsers.summaryMany";
  return t(key).replace("{count}", String(count));
}

function renderOnlineUsersSnapshot(snapshot) {
  if (!onlineUsersWidget || !onlineUsersList || !onlineUsersSummary) {
    return;
  }

  if (!snapshot) {
    return;
  }

  const currentUserId = onlineUsersWidget.dataset.currentUserId || "";
  const currentUserEmail = (onlineUsersWidget.dataset.currentUserEmail || "").trim().toUpperCase();
  const users = [...snapshot.users].sort((left, right) => {
    const leftIsCurrent = (currentUserId && left.userId === currentUserId) || (currentUserEmail && left.email.trim().toUpperCase() === currentUserEmail);
    const rightIsCurrent = (currentUserId && right.userId === currentUserId) || (currentUserEmail && right.email.trim().toUpperCase() === currentUserEmail);
    if (leftIsCurrent !== rightIsCurrent) {
      return leftIsCurrent ? -1 : 1;
    }

    return left.displayName.localeCompare(right.displayName, undefined, { sensitivity: "base" });
  });

  onlineUsersSummary.textContent = formatOnlineUsersSummary(snapshot.onlineUserCount);

  if (users.length === 0) {
    onlineUsersList.innerHTML = "<li class=\"online-users-widget__empty\">" + escapeHtml(t("onlineUsers.empty")) + "</li>";
    return;
  }

  onlineUsersList.innerHTML = users.map((user) => {
    const isCurrentUser = (currentUserId && user.userId === currentUserId) || (currentUserEmail && user.email.trim().toUpperCase() === currentUserEmail);
    const statusIcon = isCurrentUser ? "visibility" : "chat";
    const statusTitle = isCurrentUser ? t("onlineUsers.youAreOnline") : t("onlineUsers.online");
    const roleLabel = user.role ? "<span class=\"online-users-widget__role\">" + escapeHtml(user.role) + "</span>" : "";
    const connectionLabel = user.connectionCount > 1 ? "<span class=\"online-users-widget__connections\">" + escapeHtml(t("onlineUsers.tabs").replace("{count}", String(user.connectionCount))) + "</span>" : "";

    return [
      "<li class=\"online-users-widget__item" + (isCurrentUser ? " is-current-user" : "") + "\">",
      "  <span class=\"online-users-widget__avatar\">" + escapeHtml(user.initials) + "</span>",
      "  <span class=\"online-users-widget__identity\">",
      "    <strong>" + escapeHtml(user.displayName) + "</strong>",
      "    <small>" + roleLabel + connectionLabel + "</small>",
      "  </span>",
      "  <span class=\"material-symbols-outlined online-users-widget__icon\" title=\"" + escapeHtml(statusTitle) + "\" aria-hidden=\"true\">" + statusIcon + "</span>",
      "</li>"
    ].join("");
  }).join("");
}

function applyOnlineUsersChanged(payload) {
  const snapshot = normalizeOnlineUsersPayload(payload);
  if (!snapshot) {
    return;
  }

  latestOnlineUsersSnapshot = snapshot;
  renderOnlineUsersSnapshot(snapshot);
}
function formatPreviewBytes(bytes) {
  const units = ["B", "KB", "MB", "GB", "TB"];
  let value = Math.max(0, Number(bytes) || 0);
  let unitIndex = 0;
  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024;
    unitIndex += 1;
  }

  return unitIndex === 0 ? `${value} ${units[unitIndex]}` : `${value.toFixed(value >= 10 ? 1 : 2).replace(/\.0+$/, "")} ${units[unitIndex]}`;
}

function formatPreviewDate(value) {
  if (!value) {
    return "";
  }

  return new Date(value).toLocaleString(getLanguage() === "vi" ? "vi-VN" : "en-US", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit"
  });
}

function closeDocumentPreview() {
  if (!documentPreviewModal) {
    return;
  }

  documentPreviewModal.classList.remove("is-open");
  documentPreviewModal.setAttribute("aria-hidden", "true");
}

function renderDocumentPreview(document) {
  if (!documentPreviewTitle || !documentPreviewMeta || !documentPreviewBody) {
    return;
  }

  const chunks = Array.isArray(document.chunks) ? document.chunks : [];
  const embeddingLabel = document.embeddingModel
    ? `Embedding: ${document.embeddingModel}${document.embeddingDimensions ? ` (${document.embeddingDimensions} dims)` : ""}`
    : "";
  const indexedLabel = document.indexedAt ? `Indexed: ${formatPreviewDate(document.indexedAt)}` : "";
  const uploader = document.uploadedByName ? `Upload: ${document.uploadedByName}` : "Upload: Không rõ";
  const subjectOwner = document.subjectOwnerName ? `Phụ trách: ${document.subjectOwnerName}` : "Phụ trách: Chưa phân công";
  documentPreviewTitle.textContent = document.fileName || "Tài liệu";
  documentPreviewMeta.textContent = [
    document.subject,
    document.chapter,
    document.status ? `Status: ${document.status}` : "",
    indexedLabel,
    uploader,
    subjectOwner,
    embeddingLabel,
    document.chunkingStrategy ? `Chunking: ${document.chunkingStrategy}` : "",
    `${chunks.length || document.chunkCount || 0} chunks`,
    formatPreviewBytes(document.fileSizeBytes),
    formatPreviewDate(document.uploadedAt)
  ].filter(Boolean).join(" / ");

  if (chunks.length === 0) {
    documentPreviewBody.innerHTML = `
      <div class="rbl-empty-state compact">
        <strong>Chưa có nội dung index.</strong>
        <p>Tài liệu này chưa có chunk text để hiển thị.</p>
      </div>`;
    return;
  }

  const totalChars = chunks.reduce((sum, chunk) => sum + (chunk.text || "").length, 0);
  const summary = `
    <div class="rbl-index-summary">
      <article><span>Chunks</span><strong>${escapeHtml(String(chunks.length))}</strong></article>
      <article><span>Indexed chars</span><strong>${escapeHtml(String(totalChars))}</strong></article>
      <article><span>Strategy</span><strong>${escapeHtml(document.chunkingStrategy || "unknown")}</strong></article>
    </div>`;

  documentPreviewBody.innerHTML = summary + chunks.map((chunk) => `
    <article class="rbl-document-preview-chunk">
      <span>Chunk ${escapeHtml(String(chunk.chunkIndex ?? ""))}${chunk.sectionTitle ? ` / ${escapeHtml(chunk.sectionTitle)}` : ""}</span>
      <small>${[
        Number.isInteger(chunk.charStart) && Number.isInteger(chunk.charEnd) ? `${chunk.charStart}-${chunk.charEnd}` : "",
        chunk.text ? `${chunk.text.length} chars` : ""
      ].filter(Boolean).map((value) => escapeHtml(String(value))).join(" / ")}</small>
      <p>${escapeHtml(chunk.text || "")}</p>
    </article>
  `).join("");
}

async function openDocumentPreview(url) {
  if (!documentPreviewModal || !documentPreviewBody) {
    return;
  }

  documentPreviewModal.classList.add("is-open");
  documentPreviewModal.setAttribute("aria-hidden", "false");
  if (documentPreviewTitle) {
    documentPreviewTitle.textContent = "Tài liệu";
  }
  if (documentPreviewMeta) {
    documentPreviewMeta.textContent = "";
  }
  documentPreviewBody.innerHTML = `<p class="rbl-catalog-muted">Đang tải nội dung đã index...</p>`;

  try {
    const response = await fetch(url, { headers: { "Accept": "application/json" } });
    const payload = await response.json();
    if (!response.ok) {
      throw new Error(payload.error || "Could not load document preview.");
    }

    renderDocumentPreview(payload);
  } catch (error) {
    documentPreviewBody.innerHTML = `
      <div class="rbl-alert is-error">
        ${escapeHtml(error.message || "Không tải được nội dung tài liệu.")}
      </div>`;
  }
}

function bindDocumentPreviewButtons() {
  document.querySelectorAll("[data-document-preview-action], [data-document-preview-url]").forEach((button) => {
    if (button.dataset.documentPreviewBound === "true") {
      return;
    }

    button.dataset.documentPreviewBound = "true";
    button.addEventListener("click", () => {
      if (button.disabled || !button.dataset.documentPreviewUrl) {
        return;
      }

      openDocumentPreview(button.dataset.documentPreviewUrl);
    });
  });

  document.querySelectorAll("[data-document-preview-close]").forEach((button) => {
    if (button.dataset.documentPreviewCloseBound === "true") {
      return;
    }

    button.dataset.documentPreviewCloseBound = "true";
    button.addEventListener("click", closeDocumentPreview);
  });

  if (document.body.dataset.documentPreviewEscapeBound !== "true") {
    document.body.dataset.documentPreviewEscapeBound = "true";
    document.addEventListener("keydown", (event) => {
      if (event.key === "Escape" && documentPreviewModal?.classList.contains("is-open")) {
        closeDocumentPreview();
      }
    });
  }
}

function normalizeDocumentStatusPayload(payload) {
  if (!payload || typeof payload !== "object") {
    return null;
  }

  const documentId = payload.documentId || payload.DocumentId || "";
  if (!documentId) {
    return null;
  }

  return {
    documentId,
    fileName: payload.fileName || payload.FileName || "",
    subject: payload.subject || payload.Subject || "",
    chapter: payload.chapter || payload.Chapter || "",
    status: payload.status || payload.Status || "",
    chunkCount: Number(payload.chunkCount ?? payload.ChunkCount ?? 0),
    indexedAt: payload.indexedAt || payload.IndexedAt || "",
    indexError: payload.indexError || payload.IndexError || ""
  };
}

function normalizeDocumentStatus(status) {
  return (status || "").trim().toLowerCase();
}

function getDocumentStatusLabel(status) {
  switch (normalizeDocumentStatus(status)) {
    case "indexed":
      return "Indexed";
    case "processing":
      return "Processing";
    case "failed":
      return "Failed";
    default:
      return status || "Unknown";
  }
}

function getDocumentStatusClass(status) {
  switch (normalizeDocumentStatus(status)) {
    case "indexed":
      return "is-success";
    case "processing":
      return "is-running";
    case "failed":
      return "is-error";
    default:
      return "is-muted";
  }
}

function getDocumentStatusTitle(document) {
  if (normalizeDocumentStatus(document.status) === "failed" && document.indexError) {
    return document.indexError;
  }

  if (normalizeDocumentStatus(document.status) === "indexed" && document.indexedAt) {
    return `Indexed at ${formatPreviewDate(document.indexedAt)}`;
  }

  return getDocumentStatusLabel(document.status);
}

function updateDocumentKpiValue(key, delta) {
  const target = document.querySelector(`[data-document-kpi="${key}"]`);
  if (!target || delta === 0) {
    return;
  }

  const currentValue = Number.parseInt(target.textContent || "0", 10);
  target.textContent = String(Math.max(0, (Number.isNaN(currentValue) ? 0 : currentValue) + delta));
}

function updateDocumentKpis(previousStatus, nextStatus) {
  const previousKey = normalizeDocumentStatus(previousStatus);
  const nextKey = normalizeDocumentStatus(nextStatus);
  if (!previousKey || previousKey === nextKey) {
    return;
  }

  if (["indexed", "processing", "failed"].includes(previousKey)) {
    updateDocumentKpiValue(previousKey, -1);
  }

  if (["indexed", "processing", "failed"].includes(nextKey)) {
    updateDocumentKpiValue(nextKey, 1);
  }
}

function updateDocumentPreviewAction(row, document) {
  const action = row.querySelector("[data-document-preview-action]");
  if (!action) {
    return;
  }

  const isIndexed = normalizeDocumentStatus(document.status) === "indexed";
  action.disabled = !isIndexed;
  action.textContent = action.dataset.documentPreviewLabel || action.textContent || "View";
  action.title = isIndexed ? "" : getDocumentStatusTitle(document);
  if (isIndexed) {
    action.dataset.documentPreviewUrl = `/Home/DocumentPreview/${document.documentId}`;
  } else {
    delete action.dataset.documentPreviewUrl;
  }
}

function normalizeDocumentProgressPercent(value) {
  return Number.isFinite(value) ? Math.max(0, Math.min(100, value)) : 0;
}

function getDocumentProgressStageMeta(stage) {
  switch ((stage || "").trim().toLowerCase()) {
    case "queued":
      return { label: "Queued", tone: "is-queued" };
    case "extracting":
      return { label: "Extracting", tone: "is-extracting" };
    case "chunking":
      return { label: "Chunking", tone: "is-chunking" };
    case "embedding":
      return { label: "Embedding", tone: "is-embedding" };
    case "saving":
      return { label: "Saving", tone: "is-saving" };
    case "completed":
      return { label: "Completed", tone: "is-completed" };
    default:
      return { label: stage || "Processing", tone: "is-processing" };
  }
}

function formatDocumentIndexProgress(progress) {
  if (!progress) {
    return null;
  }

  const stageMeta = getDocumentProgressStageMeta(progress.stage);
  const percent = normalizeDocumentProgressPercent(progress.progressPercent);
  const message = (progress.message || "").trim();

  return {
    stage: stageMeta.label,
    tone: stageMeta.tone,
    percent,
    message,
    summary: message ? `${stageMeta.label} ${percent}% - ${message}` : `${stageMeta.label} ${percent}%`
  };
}

function applyDocumentProgressToRow(row, progress) {
  if (!row) {
    return;
  }

  const statusBadge = row.querySelector("[data-document-status]");
  const progressRow = window.document.querySelector(`tr[data-document-progress-row="${CSS.escape(row.dataset.documentId)}"]`);
  const progressElement = progressRow ? progressRow.querySelector("[data-document-progress]") : row.querySelector("[data-document-progress]");
  const progressState = formatDocumentIndexProgress(progress);
  if (!progressElement || !progressState) {
    return;
  }

  if (statusBadge) {
    statusBadge.title = progressState.message || progressState.summary;
  }

  if (progressRow) {
    progressRow.hidden = false;
  }
  progressElement.hidden = false;
  progressElement.dataset.progressTone = progressState.tone;
  progressElement.title = progressState.message || progressState.summary;
  progressElement.innerHTML = [
    '<span class="ops-document-progress__meta">',
    '  <span class="ops-document-progress__stage">' + escapeHtml(progressState.stage) + '</span>',
    '  <span class="ops-document-progress__percent">' + progressState.percent + '%</span>',
    '</span>',
    '<span class="ops-document-progress__bar" aria-hidden="true"><span class="ops-document-progress__fill" style="width:' + progressState.percent + '%"></span></span>',
    progressState.message
      ? '<span class="ops-document-progress__message">' + escapeHtml(progressState.message) + '</span>'
      : ''
  ].join("");

  row.dataset.documentProgressStage = progressState.stage;
  row.dataset.documentProgressPercent = String(progressState.percent);
}

function clearDocumentProgress(row) {
  if (!row) {
    return;
  }

  const progressRow = window.document.querySelector(`tr[data-document-progress-row="${CSS.escape(row.dataset.documentId)}"]`);
  const progressElement = progressRow ? progressRow.querySelector("[data-document-progress]") : row.querySelector("[data-document-progress]");
  if (progressElement) {
    progressElement.innerHTML = "";
    progressElement.hidden = true;
    delete progressElement.dataset.progressTone;
    progressElement.removeAttribute("title");
  }

  if (progressRow) {
    progressRow.hidden = true;
  }

  delete row.dataset.documentProgressStage;
  delete row.dataset.documentProgressPercent;
}

function applyDocumentIndexProgressChanged(payload) {
  if (!payload?.documentId) {
    return;
  }

  const rows = [...window.document.querySelectorAll(`[data-document-id="${CSS.escape(payload.documentId)}"]`)];
  rows.forEach((row) => {
    applyDocumentProgressToRow(row, payload);
    row.classList.add("is-realtime-updated");
    window.setTimeout(() => row.classList.remove("is-realtime-updated"), 1400);
  });
}

function applyOnlineUsersWidgetState() {
  if (!onlineUsersWidget || !onlineUsersToggle) {
    return;
  }

  const collapsed = localStorage.getItem(onlineUsersWidgetStateKey) === "true";
  onlineUsersWidget.classList.toggle("is-collapsed", collapsed);
  onlineUsersToggle.setAttribute("aria-expanded", String(!collapsed));
  onlineUsersToggle.setAttribute("aria-label", collapsed ? t("onlineUsers.expand") : t("onlineUsers.collapse"));
  const icon = onlineUsersToggle.querySelector(".material-symbols-outlined");
  if (icon) {
    icon.textContent = collapsed ? "expand_less" : "expand_more";
  }
}

function initOnlineUsersWidget() {
  if (!onlineUsersWidget || !onlineUsersToggle) {
    return;
  }

  applyOnlineUsersWidgetState();
  onlineUsersToggle.addEventListener("click", () => {
    const nextCollapsed = !onlineUsersWidget.classList.contains("is-collapsed");
    localStorage.setItem(onlineUsersWidgetStateKey, String(nextCollapsed));
    applyOnlineUsersWidgetState();
  });
}
function updateDocumentRow(row, document) {
  row.dataset.documentStatusValue = document.status;

  const statusBadge = row.querySelector("[data-document-status]");
  if (statusBadge) {
    statusBadge.classList.remove("is-success", "is-running", "is-error", "is-muted");
    statusBadge.classList.add(getDocumentStatusClass(document.status));
    statusBadge.textContent = getDocumentStatusLabel(document.status);
    statusBadge.title = getDocumentStatusTitle(document);
  }

  const chunks = row.querySelector("[data-document-chunks]");
  if (chunks) {
    chunks.textContent = `${document.chunkCount || 0} chunks`;
  }

  const treeMeta = row.querySelector("[data-document-tree-meta]");
  if (treeMeta) {
    const sizeLabel = row.dataset.documentSizeLabel || "";
    treeMeta.textContent = [getDocumentStatusLabel(document.status), `${document.chunkCount || 0} chunks`, sizeLabel]
      .filter(Boolean)
      .join(" · ");
  }

  clearDocumentProgress(row);
  updateDocumentPreviewAction(row, document);
  row.classList.add("is-realtime-updated");
  window.setTimeout(() => row.classList.remove("is-realtime-updated"), 1400);
}

function applyDocumentStatusChanged(payload) {
  const document = normalizeDocumentStatusPayload(payload);
  if (!document) {
    return;
  }

  const rows = [...window.document.querySelectorAll(`[data-document-id="${CSS.escape(document.documentId)}"]`)];
  if (rows.length === 0) {
    return;
  }

  const previousStatus = rows[0].dataset.documentStatusValue || rows[0].querySelector("[data-document-status]")?.textContent || "";
  updateDocumentKpis(previousStatus, document.status);
  rows.forEach((row) => updateDocumentRow(row, document));
  bindDocumentPreviewButtons();
}

async function startDocumentStatusRealtime() {
  if (!window.signalR?.HubConnectionBuilder || (!documentsPage && !onlineUsersWidget)) {
    return;
  }

  const connection = new window.signalR.HubConnectionBuilder()
    .withUrl("/hubs/documents")
    .withAutomaticReconnect()
    .build();

  if (documentsPage) {
    connection.on("documentStatusChanged", applyDocumentStatusChanged);
    connection.on("documentIndexProgressChanged", applyDocumentIndexProgressChanged);
  }

  if (onlineUsersWidget) {
    connection.on("onlineUsersChanged", applyOnlineUsersChanged);
    connection.onreconnecting(() => setOnlineUsersConnectionState("reconnecting"));
    connection.onreconnected(async () => {
      setOnlineUsersConnectionState("connected");
      try {
        applyOnlineUsersChanged(await connection.invoke("GetOnlineUsersAsync"));
      } catch {
        // Ignore refresh failures after reconnect.
      }
    });
    connection.onclose(() => setOnlineUsersConnectionState("disconnected"));
  }

  async function start() {
    try {
      await connection.start();
      if (onlineUsersWidget) {
        setOnlineUsersConnectionState("connected");
        try {
          applyOnlineUsersChanged(await connection.invoke("GetOnlineUsersAsync"));
        } catch {
          // Initial snapshot is best effort only.
        }
      }
    } catch {
      if (onlineUsersWidget) {
        setOnlineUsersConnectionState("disconnected");
      }
      window.setTimeout(start, 5000);
    }
  }

  await start();
}
async function refreshSessionList() {
  if (!chatSessionList) {
    return;
  }

  try {
    const response = await fetch("/Home/ChatSessions");
    if (!response.ok) {
      return;
    }

    renderSessionList(await response.json());
  } catch {
    // Session history is helpful, but chat should still work if it cannot refresh.
  }
}

async function loadChatSession(sessionId) {
  if (!sessionId) {
    return;
  }

  const response = await fetch(`/Home/ChatSession/${sessionId}`);
  if (!response.ok) {
    return;
  }

  const session = await response.json();
  setSessionId(session.id);
  if (activeSessionTitle) {
    activeSessionTitle.textContent = getSessionTitle(session);
  }
  renderSessionMessages(session.messages || []);
  questionInput?.focus();
}

async function postSessionJson(url, body) {
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body)
  });
  const payload = await response.json().catch(() => ({}));
  if (!response.ok) {
    throw new Error(payload.error || t("chat.sessionActionError"));
  }

  return payload;
}

async function renameChatSession(sessionId) {
  const currentTitle = document
    .querySelector(`.chat-session-item[data-session-id="${CSS.escape(sessionId)}"] span`)
    ?.textContent
    ?.trim() || "";
  const title = window.prompt(t("chat.renamePrompt"), currentTitle);
  if (title === null) {
    return;
  }

  const normalizedTitle = title.trim();
  if (!normalizedTitle) {
    return;
  }

  const session = await postSessionJson("/Home/RenameChatSession", { sessionId, title: normalizedTitle });
  if (getSessionId() === session.id && activeSessionTitle) {
    activeSessionTitle.textContent = getSessionTitle(session);
  }
  await refreshSessionList();
}

async function toggleChatSessionStar(sessionId, isCurrentlyStarred) {
  await postSessionJson("/Home/StarChatSession", {
    sessionId,
    isStarred: !isCurrentlyStarred
  });
  await refreshSessionList();
}

async function deleteChatSession(sessionId) {
  if (!window.confirm(t("chat.deleteConfirm"))) {
    return;
  }

  await postSessionJson("/Home/DeleteChatSession", { sessionId });
  if (getSessionId() === sessionId) {
    const response = await fetch("/Home/CreateChatSession", { method: "POST" });
    if (response.ok) {
      const session = await response.json();
      setSessionId(session.id);
    } else {
      setSessionId(createSessionId());
    }

    if (activeSessionTitle) {
      activeSessionTitle.textContent = t("chat.defaultSessionTitle");
    }
    renderWelcomeMessage();
  }

  await refreshSessionList();
}

function bindSessionButtons() {
  document.querySelectorAll(".chat-session-item").forEach((button) => {
    button.addEventListener("click", () => {
      loadChatSession(button.dataset.sessionId);
    });
  });

  document.querySelectorAll("[data-session-menu-toggle]").forEach((button) => {
    button.addEventListener("click", (event) => {
      event.stopPropagation();
      const entry = button.closest("[data-session-entry]");
      const menu = entry?.querySelector("[data-session-menu]");
      if (!menu) {
        return;
      }

      const willOpen = !menu.classList.contains("is-open");
      closeSessionMenus(menu);
      menu.classList.toggle("is-open", willOpen);
    });
  });

  document.querySelectorAll("[data-session-action]").forEach((button) => {
    button.addEventListener("click", async (event) => {
      event.stopPropagation();
      const sessionId = button.dataset.sessionId;
      const action = button.dataset.sessionAction;
      closeSessionMenus();
      if (!sessionId || !action) {
        return;
      }

      try {
        if (action === "rename") {
          await renameChatSession(sessionId);
        } else if (action === "star") {
          await toggleChatSessionStar(sessionId, button.dataset.sessionStarred === "true");
        } else if (action === "delete") {
          await deleteChatSession(sessionId);
        }
      } catch (error) {
        appendMessageTo(chatMessages, "assistant", error.message || t("chat.sessionActionError"));
      }
    });
  });
}

function appendMessageTo(target, role, content, citations = []) {
  if (!target) {
    return null;
  }

  if (role === "user") {
    target.querySelector(".chat-empty-state")?.remove();
  }

  const wrapper = document.createElement("div");
  wrapper.className = `message ${role}`;

  const bubble = document.createElement("div");
  bubble.className = "bubble";
  bubble.textContent = content;

  wrapper.appendChild(bubble);
  appendCitationsToMessage(wrapper, citations);
  target.appendChild(wrapper);
  target.scrollTop = target.scrollHeight;
  return wrapper;
}

function appendCitationsToMessage(messageWrapper, citations) {
  const sourceItems = Array.isArray(citations)
    ? citations.filter((citation) => citation && typeof citation === "object")
    : [];

  if (!messageWrapper || sourceItems.length === 0) {
    return;
  }

  const seenSources = new Set();
  const compactSources = [];
  for (const citation of sourceItems) {
    const fileName = citation.fileName || citation.FileName || "";
    const subject = citation.subject || citation.Subject || "";
    const chapter = citation.chapter || citation.Chapter || "";
    const chunkIndex = citation.chunkIndex ?? citation.ChunkIndex;
    const sourceKey = `${fileName || subject}|${chapter}|${chunkIndex ?? ""}`;
    if (seenSources.has(sourceKey)) {
      continue;
    }

    seenSources.add(sourceKey);
    compactSources.push(citation);
    if (compactSources.length === 3) {
      break;
    }
  }

  if (compactSources.length === 0) {
    return;
  }

  const list = document.createElement("div");
  list.className = "citations compact-citations";
  list.setAttribute("aria-label", "Sources");

  const label = document.createElement("span");
  label.className = "citation-label";
  label.textContent = getLanguage() === "vi" ? "Nguồn:" : "Sources:";
  list.appendChild(label);

  compactSources.forEach((citation) => {
    const item = document.createElement("span");
    item.className = "citation citation-source";
    const fileName = citation.fileName || citation.FileName || "";
    const chunkIndex = citation.chunkIndex ?? citation.ChunkIndex;
    const metaValues = [
      citation.subject || citation.Subject,
      citation.chapter || citation.Chapter
    ].filter(Boolean);
    const safeSource = fileName || metaValues.join(" / ") || (getLanguage() === "vi" ? "Nguồn nội bộ" : "Internal source");
    item.textContent = fileName && chunkIndex ? `${safeSource} / chunk ${chunkIndex}` : safeSource;
    item.title = metaValues.join(" / ");

    list.appendChild(item);
  });

  messageWrapper.appendChild(list);
}

function renderClarificationOptions(messageWrapper, options, originalQuestion) {
  const subjects = Array.isArray(options)
    ? options.filter((subject) => typeof subject === "string" && subject.trim().length > 0).slice(0, 6)
    : [];

  if (!messageWrapper || subjects.length === 0) {
    return;
  }

  const actions = document.createElement("div");
  actions.className = "chat-clarification-options";
  subjects.forEach((subject) => {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "chat-clarification-chip";
    button.textContent = subject;
    button.addEventListener("click", () => {
      setSelectedSubjectFilter(subject);
      if (questionInput && chatForm) {
        questionInput.value = originalQuestion;
        chatForm.requestSubmit();
      }
    });
    actions.appendChild(button);
  });

  messageWrapper.appendChild(actions);
  messageWrapper.parentElement.scrollTop = messageWrapper.parentElement.scrollHeight;
}

async function submitChatQuestion(input, messagesTarget, focusAfter = true) {
  const question = input?.value.trim();
  if (!question || !messagesTarget) {
    return false;
  }

  rememberAskedQuestion(question);
  advanceRelatedRotation();
  updateSuggestionButtons();
  renderRelatedQuestions();
  appendMessageTo(messagesTarget, "user", question);
  input.value = "";
  const loadingMessage = appendMessageTo(messagesTarget, "assistant", t("chat.loading"));

  try {
    const response = await fetch("/Home/Ask", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        sessionId: getSessionId(),
        question,
        subjectFilter: getSelectedSubjectFilter(),
        language: getLanguage()
      })
    });

    const payload = await response.json();
    loadingMessage?.remove();

    if (!response.ok) {
      appendMessageTo(messagesTarget, "assistant", payload.error || t("chat.requestError"));
      return false;
    }

    setSessionId(payload.sessionId);
    rememberAskedQuestion(question);
    if (activeSessionTitle && (!activeSessionTitle.textContent?.trim() || activeSessionTitle.textContent.trim() === t("chat.defaultSessionTitle"))) {
      activeSessionTitle.textContent = question.length <= 56 ? question : `${question.slice(0, 56)}...`;
    }
    const answerMessage = appendMessageTo(messagesTarget, "assistant", payload.answer, payload.citations || []);
    if (payload.needsClarification && Array.isArray(payload.subjectOptions)) {
      renderClarificationOptions(answerMessage, payload.subjectOptions, question);
    }

    advanceRelatedRotation();
    renderRelatedQuestions();
    refreshSessionList();
    return true;
  } catch {
    loadingMessage?.remove();
    appendMessageTo(messagesTarget, "assistant", t("chat.connectionError"));
    return false;
  } finally {
    if (focusAfter) {
      input.focus();
    }
  }
}

document.querySelectorAll("[data-language-option]").forEach((button) => {
  button.addEventListener("click", () => {
    setLanguage(button.dataset.languageOption);
  });
});

document.addEventListener("click", (event) => {
  if (!event.target.closest?.("[data-session-entry]")) {
    closeSessionMenus();
  }
});

document.addEventListener("keydown", (event) => {
  if (event.key === "Escape") {
    closeSessionMenus();
  }
});

if (newSessionButton) {
  newSessionButton.addEventListener("click", async () => {
    try {
      const response = await fetch("/Home/CreateChatSession", { method: "POST" });
      if (response.ok) {
        const session = await response.json();
        setSessionId(session.id);
      } else {
        setSessionId(createSessionId());
      }
    } catch {
      setSessionId(createSessionId());
    }

    if (activeSessionTitle) {
      activeSessionTitle.textContent = t("chat.defaultSessionTitle");
    }
    renderWelcomeMessage();
    refreshSessionList();
  });
}

if (documentDropzone && documentFileInput && documentFileName) {
  function updateSelectedFileName(files) {
    const file = files?.[0];
    documentFileName.textContent = file ? file.name : t("documents.dropzoneDefault");
    documentDropzone.classList.toggle("has-file", Boolean(file));
  }

  documentFileInput.addEventListener("change", () => {
    updateSelectedFileName(documentFileInput.files);
  });

  ["dragenter", "dragover"].forEach((eventName) => {
    documentDropzone.addEventListener(eventName, (event) => {
      event.preventDefault();
      documentDropzone.classList.add("is-dragover");
    });
  });

  ["dragleave", "drop"].forEach((eventName) => {
    documentDropzone.addEventListener(eventName, () => {
      documentDropzone.classList.remove("is-dragover");
    });
  });

  documentDropzone.addEventListener("drop", (event) => {
    event.preventDefault();
    if (event.dataTransfer?.files?.length) {
      documentFileInput.files = event.dataTransfer.files;
      updateSelectedFileName(documentFileInput.files);
    }
  });
}

function initAssistantLauncherDrag() {
  if (!assistantLauncher || !assistantLauncherButton) {
    return;
  }

  let startX = 0;
  let startY = 0;
  let startLeft = 0;
  let startTop = 0;
  let lastX = 0;
  let didDrag = false;
  let suppressClick = false;

  function clamp(value, min, max) {
    return Math.min(Math.max(value, min), max);
  }

  function moveLauncher(left, top, deltaX) {
    const rect = assistantLauncher.getBoundingClientRect();
    const maxLeft = window.innerWidth - rect.width - 10;
    const maxTop = window.innerHeight - rect.height - 10;
    assistantLauncher.style.left = `${clamp(left, 10, maxLeft)}px`;
    assistantLauncher.style.top = `${clamp(top, 10, maxTop)}px`;
    assistantLauncher.style.right = "auto";
    assistantLauncher.style.bottom = "auto";
    assistantLauncher.style.setProperty("--launcher-stretch-x", String(1 + Math.min(Math.abs(deltaX) / 160, 0.16)));
    assistantLauncher.style.setProperty("--launcher-stretch-y", String(1 - Math.min(Math.abs(deltaX) / 240, 0.08)));
    assistantLauncher.style.setProperty("--launcher-rotate", `${clamp(deltaX / 12, -8, 8)}deg`);
  }

  assistantLauncherButton.addEventListener("pointerdown", (event) => {
    if (event.button !== 0) {
      return;
    }

    const rect = assistantLauncher.getBoundingClientRect();
    startX = event.clientX;
    startY = event.clientY;
    lastX = event.clientX;
    startLeft = rect.left;
    startTop = rect.top;
    didDrag = false;
    event.preventDefault();
    assistantLauncher.classList.add("is-dragging");
    assistantLauncher.classList.remove("is-released");
    assistantLauncherButton.setPointerCapture?.(event.pointerId);
  });

  assistantLauncherButton.addEventListener("pointermove", (event) => {
    if (!assistantLauncher.classList.contains("is-dragging")) {
      return;
    }

    const deltaX = event.clientX - startX;
    const deltaY = event.clientY - startY;
    if (Math.hypot(deltaX, deltaY) > 5) {
      didDrag = true;
    }

    if (!didDrag) {
      return;
    }

    event.preventDefault();
    moveLauncher(startLeft + deltaX, startTop + deltaY, event.clientX - lastX);
    lastX = event.clientX;
  });

  function finishDrag(event) {
    if (!assistantLauncher.classList.contains("is-dragging")) {
      return;
    }

    assistantLauncher.classList.remove("is-dragging");
    assistantLauncher.classList.add("is-released");
    assistantLauncher.style.setProperty("--launcher-stretch-x", "1");
    assistantLauncher.style.setProperty("--launcher-stretch-y", "1");
    assistantLauncher.style.setProperty("--launcher-rotate", "0deg");
    assistantLauncherButton.releasePointerCapture?.(event.pointerId);

    if (didDrag) {
      suppressClick = true;
      window.setTimeout(() => {
        suppressClick = false;
      }, 0);
    }

    window.setTimeout(() => {
      assistantLauncher.classList.remove("is-released");
    }, 280);
  }

  assistantLauncherButton.addEventListener("pointerup", finishDrag);
  assistantLauncherButton.addEventListener("pointercancel", finishDrag);
  assistantLauncherButton.addEventListener("click", (event) => {
    event.preventDefault();

    const clickDistance = Math.hypot(event.clientX - startX, event.clientY - startY);
    if (suppressClick || clickDistance > 5) {
      return;
    }

    window.location.href = assistantLauncherButton.href;
  });
}

function bindSuggestionButtons() {
  document.querySelectorAll("[data-question]").forEach((button) => {
    if (button.dataset.questionBound === "true") {
      return;
    }

    button.dataset.questionBound = "true";
    button.addEventListener("click", () => {
      if (!questionInput) {
        return;
      }

      questionInput.value = button.dataset.question || "";
      questionInput.focus();
      rememberAskedQuestion(questionInput.value);
      updateSuggestionButtons();
      renderRelatedQuestions();
      if (button.classList.contains("related-question-chip")) {
        chatForm?.requestSubmit();
      }
    });
  });
}

function initAdminCreateUserForm() {
  document.querySelectorAll("[data-admin-user-role-subject-form]").forEach((form) => {
    if (form.dataset.roleSubjectBound === "true") {
      return;
    }

    const roleSelect = form.querySelector("[data-admin-role-select]");
    const subjectPicker = form.querySelector("[data-lecturer-subject-picker]");
    if (!roleSelect || !subjectPicker) {
      return;
    }

    form.dataset.roleSubjectBound = "true";
    const subjectInputs = Array.from(subjectPicker.querySelectorAll("input[name='SubjectIds']"));
    const syncSubjectPicker = () => {
      const isLecturer = (roleSelect.value || "").toLowerCase() === "lecturer";
      subjectPicker.classList.toggle("is-hidden", !isLecturer);
      subjectInputs.forEach((input) => {
        input.disabled = !isLecturer;
        if (!isLecturer) {
          input.checked = false;
        }
      });
    };

    roleSelect.addEventListener("change", syncSubjectPicker);
    syncSubjectPicker();
  });
}

function initConfirmForms() {
  document.querySelectorAll("form[data-confirm], form[data-confirm-en], form[data-confirm-vi]").forEach((form) => {
    if (form.dataset.confirmBound === "true") {
      return;
    }

    form.dataset.confirmBound = "true";
    form.addEventListener("submit", (event) => {
      const message = getLanguage() === "vi"
        ? (form.dataset.confirmVi || form.dataset.confirm || "Bạn có chắc muốn tiếp tục?")
        : (form.dataset.confirmEn || form.dataset.confirm || "Are you sure you want to continue?");
      if (!window.confirm(message)) {
        event.preventDefault();
      }
    });
  });
}

function initSubjectCards() {
  document.querySelectorAll("[data-subject-detail-url]").forEach((card) => {
    if (card.dataset.subjectCardBound === "true") {
      return;
    }

    card.dataset.subjectCardBound = "true";
    card.addEventListener("click", (event) => {
      const url = card.dataset.subjectDetailUrl;
      if (!url) {
        return;
      }

      event.preventDefault();
      window.location.href = url;
    });

    card.addEventListener("keydown", (event) => {
      if (event.key !== "Enter" && event.key !== " ") {
        return;
      }

      const url = card.dataset.subjectDetailUrl;
      if (!url) {
        return;
      }

      event.preventDefault();
      window.location.href = url;
    });
  });
}

function initAdminRoleUpdateForms() {
  document.querySelectorAll("[data-admin-role-update-select]").forEach((select) => {
    if (select.dataset.roleUpdateBound === "true") {
      return;
    }

    select.dataset.roleUpdateBound = "true";
    select.dataset.previousValue = select.value || "";
    select.addEventListener("change", () => {
      const form = select.closest("[data-admin-role-update-form]");
      if (!form) {
        return;
      }

      const nextRole = select.value || "";
      const userLabel = select.dataset.adminRoleUser || "this user";
      const confirmMessage = t("admin.confirmRoleChange")
        .replace("{user}", userLabel)
        .replace("{role}", nextRole);
      if (!window.confirm(confirmMessage)) {
        select.value = select.dataset.previousValue || "";
        return;
      }

      select.dataset.previousValue = nextRole;
      form.requestSubmit();
    });
  });
}

bindSuggestionButtons();
bindSessionButtons();
bindSubjectFilterChips();
applyInitialSubjectFromUrl();
bindDocumentPreviewButtons();
initSubjectCards();
initAdminCreateUserForm();
initConfirmForms();
initAdminRoleUpdateForms();
initAssistantLauncherDrag();
initOnlineUsersWidget();
markActiveSession(getSessionId());
applyLanguage();
startDocumentStatusRealtime();

if (chatForm) {
  chatForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (isSending) {
      return;
    }

    isSending = true;
    try {
      await submitChatQuestion(questionInput, chatMessages);
    } finally {
      isSending = false;
    }
  });
}

if (questionInput && chatForm) {
  questionInput.addEventListener("keydown", (event) => {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      chatForm.requestSubmit();
    }
  });
}
