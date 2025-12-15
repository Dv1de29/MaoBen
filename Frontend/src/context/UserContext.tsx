import React, { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import type { UserProfileApiType, UserProfileType } from '../assets/types'; // Import your types

// 1. Define the Shape of the Context
interface UserContextType {
    user: UserProfileType;
    loading: boolean;
    refreshUser: () => Promise<void>; // Function to force a re-fetch
    logout: () => void;
}


const GUEST_USER: UserProfileType = {
    username: "",
    description: "",
    email: "",
    profilePictureUrl: "/assets/img/no_user.jpg", // Default image always ready
    privacy: false,
    posts: [],
};



// 2. Create the Context
const UserContext = createContext<UserContextType | undefined>(undefined);

// 3. Create the Provider Component
export const UserProvider = ({ children }: { children: ReactNode }) => {
    const [user, setUser] = useState<UserProfileType>(GUEST_USER);
    const [loading, setLoading] = useState(true);

    const fetchUser = async () => {
        const token = sessionStorage.getItem("userToken");
        
        // If no token, stop loading and do nothing
        if (!token) {
            setUser(GUEST_USER);
            setLoading(false);
            return;
        }   

        try {
            // NOTE: Use your proxy path /api/...
            const res = await fetch("/api/Profile", {
                method: "GET",
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            });

            if (res.ok) {
                const data: UserProfileApiType = await res.json();
                setUser({
                    ...data,
                    posts: [],
                });
            } else {
                // If token is invalid (401), clear it
                if (res.status === 401) logout();
            }
        } catch (error) {
            console.error("Error fetching user context:", error);
        } finally {
            setLoading(false);
        }
    };

    const logout = () => {
        sessionStorage.clear();
        setUser(GUEST_USER);
        // Optional: Redirect to login logic here if needed
    };

    // Fetch on mount
    useEffect(() => {
        fetchUser();
    }, []);

    return (
        <UserContext.Provider value={{ user, loading, refreshUser: fetchUser, logout }}>
            {children}
        </UserContext.Provider>
    );
};

// 4. Custom Hook for easy access
// eslint-disable-next-line react-refresh/only-export-components
export const useUser = () => {
    const context = useContext(UserContext);
    if (!context) {
        throw new Error("useUser must be used within a UserProvider");
    }
    return context;
};