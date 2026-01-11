/* eslint-disable @typescript-eslint/no-explicit-any */
import React, { useEffect, useState } from 'react';
import '../styles/GroupsPage.css';

interface GroupDto {
    id: number;
    name: string;
    description: string;
    ownerUsername: string;
    isUserMember: boolean;
}

function GroupsPage() {
    const [groups, setGroups] = useState<GroupDto[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

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

                if (!response.ok) {
                    throw new Error(`Error: ${response.status} ${response.statusText}`);
                }

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

    const handleJoinGroup = (group_id: number) => {
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

                if (!response.ok) {
                    throw new Error(`Error: ${response.status} ${response.statusText}`);
                }

                setGroups(prev => prev.map(g => {
                    if ( g.id === group_id ){
                        return {
                            ...g,
                            isUserMember: true,
                        }
                    }
                    return g;
                }))

            } catch (err: any) {
                setError(err.message || 'Failed to join');
            }
        }

        fetchJoin();
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
                        <li key={group.id} className="group-card">
                            <div className="group-header">
                                <h3>{group.name}</h3>
                                {group.isUserMember && (
                                    <span className="badge-member">Member</span>
                                )}
                            </div>
                            
                            <p className="group-desc">{group.description}</p>
                            
                            <div className="group-footer">
                                {/* Join Button Logic */}
                                {!group.isUserMember && (
                                    <button 
                                        className="btn-join"
                                        onClick={() => handleJoinGroup(group.id)}
                                    >
                                        Join Group
                                    </button>
                                )}

                                {/* Owner Info with Icon */}
                                <div className="owner-info">
                                    <svg className="icon-user" viewBox="0 0 24 24">
                                        <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
                                    </svg>
                                    <span>Owner: {group.ownerUsername}</span>
                                </div>
                            </div>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}

export default GroupsPage;