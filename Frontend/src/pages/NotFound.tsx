

function NotFound(){
    return(
        <>
            <div className="notfound-wrapper" style={{width: '100%', height: '100%' , display: 'flex', justifyContent: 'center', alignItems: 'center', flexFlow: 'column'}}>
                <span style={{fontSize: '40px', color: 'white'}}>404</span>
                <br />
                <span style={{color: 'white'}}>Page not found</span>
            </div>
        </>
    )
}

export default NotFound;