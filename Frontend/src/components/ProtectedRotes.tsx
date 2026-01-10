import type { JSX } from 'react';
import { Navigate } from 'react-router-dom';

// This component wraps your pages
const ProtectedRoute = ({ children }: { children: JSX.Element }) => {
    // 1. Get your variable (e.g., your userToken)
    const isAuthenticated = !!sessionStorage.getItem("userToken");

    // 2. If the variable is false/missing, redirect to Login
    if (!isAuthenticated) {
        // 'replace' prevents them from hitting Back button to return here
        return <Navigate to="/login" replace />;
    }

    // 3. Otherwise, render the requested page
    return children;
};

export default ProtectedRoute;  