import React, { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import type { UserProfileApiType, UserProfileType } from '../assets/types'; // Import your types


interface UserContextType {
    user: UserProfileType;
    loading: boolean;
    refreshUser: () => Promise<void>; // Function to force a re-fetch
    logout: () => void;
}


const GUEST_USER: UserProfileType = {
    name: "Guest",
    username: "guest",
    description: "",
    email: "",
    profilePictureUrl: "/assets/img/no_user.jpg", // Default image always ready
    privacy: false,
    followingCount: 0,
    followersCount: 0,
    posts: [],
};



const UserContext = createContext<UserContextType | undefined>(undefined);


export const UserProvider = ({ children }: { children: ReactNode }) => {
    const [user, setUser] = useState<UserProfileType>(GUEST_USER);
    const [loading, setLoading] = useState(true);

    const fetchUser = async () => {
        const token = sessionStorage.getItem("userToken");
        
        if (!token) {
            sessionStorage.setItem("userRole", "Guest");
            setUser(GUEST_USER);
            setLoading(false);
            return;
        }   

        try {
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
        sessionStorage.setItem("userRole", "Guest");
    };

    useEffect(() => {
        fetchUser();
    }, []);

    return (
        <UserContext.Provider value={{ user, loading, refreshUser: fetchUser, logout }}>
            {children}
        </UserContext.Provider>
    );
};

// eslint-disable-next-line react-refresh/only-export-components
export const useUser = () => {
    const context = useContext(UserContext);
    if (!context) {
        throw new Error("useUser must be used within a UserProvider");
    }
    return context;
};