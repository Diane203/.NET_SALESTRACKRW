USE salestrackrw;

-- Verify the columns required by the signup form.
DESCRIBE users;

-- Public signup accounts must use the Cashier role.
-- The application performs this insert automatically after form validation.
-- This is an optional manual example:
-- INSERT INTO users (username, password_hash, full_name, role)
-- VALUES ('lecturer', SHA2('lecturer123', 256), 'Lecturer Account', 'Cashier');

-- Check registered accounts and their roles.
SELECT user_id, username, full_name, role, is_active, created_at
FROM users
ORDER BY created_at DESC;
