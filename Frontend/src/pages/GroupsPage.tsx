/* eslint-disable @typescript-eslint/no-explicit-any */
import React, { useEffect, useState } from 'react';
import '../styles/GroupsPage.css';
import { Link, useNavigate } from 'react-router-dom';
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
// Added faTrash import
import { faTrash } from '@fortawesome/free-solid-svg-icons';

interface GroupDto {
    id: number;
    name: string;
    description: string;
    ownerUsername: string;
    isUserMember: boolean;
}

function GroupsPage() {
    const navigate = useNavigate();
    const [groups, setGroups] = useState<GroupDto[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    // Current user context (simple fetch from session for now)
    const myUsername = sessionStorage.getItem("userName");

    useEffect(() => {
        const fetchGroups = async () => {
            const token = sessionStorage.getItem("userToken");
            try {
                const response = await fetch('/api/Groups', {
                    method: "GET",
                    headers: { 
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json'
                    }
                });

                if (!response.ok) throw new Error(`Error: ${response.status} ${response.statusText}`);

                const data: GroupDto[] = await response.json();
                setGroups(data);
            } catch (err: any) {
                setError(err.message || 'Failed to fetch groups');
            } finally {
                setIsLoading(false);
            }
        };

        fetchGroups();
    }, []);

    const handleJoinGroup = (group_id: number, owner: string) => {
        const fetchJoin = async () => {
            const token = sessionStorage.getItem("userToken");
            try {
                const response = await fetch(`/api/Groups/${group_id}/join`, {
                    method: "POST",
                    headers: { 
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json'
                    }
                });

                if (!response.ok) throw new Error(`Error: ${response.status} ${response.statusText}`);

                setGroups(prev => prev.map(g => {
                    if (g.id === group_id) {
                        return { ...g, isUserMember: true }
                    }
                    return g;
                }))
            } catch (err: any) {
                setError(err.message || 'Failed to join');
            }
        }
        fetchJoin();
    }

    // New Handler for Leaving/Deleting
    const handleLeaveGroup = async (group_id: number, groupName: string) => {
        if(!window.confirm(`Are you sure you want to leave the group "${groupName}"?`)) return;

        const token = sessionStorage.getItem("userToken");
        // Adjust endpoint based on your backend logic for "leaving"
        // Usually: DELETE /api/Groups/{id}/members/{myUsername}
        try {
            const response = await fetch(`/api/Groups/${group_id}/members/${myUsername}`, {
                method: "DELETE",
                headers: { 
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.ok) {
                // Optimistically update UI: set isUserMember to false
                setGroups(prev => prev.map(g => {
                    if (g.id === group_id) return { ...g, isUserMember: false };
                    return g;
                }));
            }
        } catch (error) {
            console.error("Failed to leave group", error);
        }
    }

    const handleAccesGroup = (id: number, owner: string) => {
        if (myUsername !== owner) return;
        navigate(`/groups/manage/${id}`);
    }

    if (isLoading) return <div className="groups-container">Loading groups...</div>;
    if (error) return <div className="groups-container text-red-500">Error: {error}</div>;

    return (
        <div className="groups-container">
            <h2>All Groups</h2>
            
            {groups.length === 0 ? (
                <p>No groups found.</p>
            ) : (
                <ul className="groups-list">
                    {groups.map((group) => (
                        <li key={group.id} className="group-card" onClick={(e) => {handleAccesGroup(group.id, group.ownerUsername)}}>
                            <div className="group-header">
                                <h3>{group.name}</h3>
                                {group.isUserMember && (
                                    <span className="badge-member">Member</span>
                                )}
                            </div>
                            
                            <p className="group-desc">{group.description}</p>
                            
                            <div className="group-footer">
                                {!group.isUserMember && (
                                    <button 
                                        className="btn-join"
                                        onClick={(e) => {handleJoinGroup(group.id, group.ownerUsername); e.stopPropagation();}}
                                    >
                                        Join Group
                                    </button>
                                )}

                                <div className="owner-info">
                                    <svg className="icon-user" viewBox="0 0 24 24">
                                        <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
                                    </svg>
                                    <span>Owner: {group.ownerUsername}</span>
                                </div>

                                {/* NEW: Delete/Leave Icon for Members */}
                                {group.ownerUsername !== myUsername && group.isUserMember && (
                                    <button 
                                        className="btn-icon-leave"
                                        title="Leave Group"
                                        onClick={(e) => {
                                            e.stopPropagation(); // Prevent card click
                                            handleLeaveGroup(group.id, group.name);
                                        }}
                                    >
                                        <FontAwesomeIcon icon={faTrash} />
                                    </button>
                                )}
                            </div>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}

export default GroupsPage;