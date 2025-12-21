import { useParams } from "react-router-dom";
import Post from "../components/Post";

function PostPage(){
    const { post_id } = useParams()

    console.log(post_id)

    return (
        <>
            {/* <Post></Post> */}
        </>
    )
}

export default PostPage;