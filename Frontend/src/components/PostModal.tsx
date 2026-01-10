// components/PostModal.js
import React, { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import Post from './Post';

import '../styles/PostModal.css'; // You'll need CSS for fixed positioning
import type { PostType, PostApiType } from '../assets/types';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faXmark } from '@fortawesome/free-solid-svg-icons';

function PostModal() {
    const navigate = useNavigate();

    const { post_id } = useParams();

    const [post, setPost] = useState<PostType | null>(null)

    // Close modal creates a "back" navigation
    const onClose = () => navigate(-1); 


    /// fetch the Post
    useEffect(() => {
        const fetchPost = async () => {
            const token = sessionStorage.getItem("userToken")

            try{
                const res = await fetch(`/api/Posts/${post_id}`, {
                    method: "GET",
                    headers: {
                        'Authorization': `Bearer ${token}`,
                    }
                })

                if ( !res.ok ){
                    throw new Error(`REsponse Error: ${res.status}, ${res.statusText}`)
                }

                const data: PostApiType = await res.json();

                setPost({
                        id: data.id,
                        owner: data.owner,
                        img_path: data.image_path,
                        description: data.description,
                        nr_likes: data.nr_likes,
                        has_liked: data.has_liked,
                        nr_comm: data.nr_comms,
                        created: data.created,
                        username: data.username,
                        user_image_path: data.user_image_path,
                })
            } catch(e){
                console.error("Error in getting Post: ", e);
            }
        }

        fetchPost();

    }, [post_id])

    const handleLike = useCallback((id: number, likeState: boolean) => {
        const token = sessionStorage.getItem("userToken")
    
        setPost(p => {
            if ( p ) {
                let newCount = p.nr_likes;
                    
                    // Safety check: Don't double count if state is already consistent
                if (likeState && !p.has_liked) {
                    newCount++;
                } else if (!likeState && p.has_liked) {
                    newCount--;
                }
                return{
                    ...p,
                    has_liked: likeState,
                    nr_likes: newCount < 0 ? 0 : newCount,
                }
            }
            return p;
        })
    
        fetch(`/api/Likes/toggle/${id}?likeState=${likeState}`, {
            method: "POST",
            keepalive: true,
            headers: {
                'Authorization': `Bearer ${token}`,
            }
        }).catch(e => {
            console.error("HandleLike Error: ", e);
        });
    
    }, [])

    return (
        <div className="modal-overlay" onClick={onClose}>
            {/* Added styling to stop propagation on the whole content box */}
                {/* Close Button - Now Sticky */}
                <div className="btn-close-wrapper">
                    <button className="close-btn" onClick={onClose}>
                        <FontAwesomeIcon icon={faXmark} />
                    </button>

                </div>
            <div className="modal-content" onClick={e => e.stopPropagation()}>
                

                {post && (
                    <Post post={post} onToggleLike={handleLike} />
                )}
            </div>
        </div>
    );
}

export default PostModal;