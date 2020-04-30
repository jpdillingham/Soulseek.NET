import React, { Component } from 'react'
import { Button, Form, Grid, Header, Icon, Segment, Checkbox } from 'semantic-ui-react'

const initialState = {
    rememberMe: true
}

class LoginForm extends Component {
    state = initialState;

    toggleRememberMe = () => {
        this.setState({ rememberMe: !this.state.rememberMe });
    }

    render = () => {
        const { onLoginAttempt } = this.props;
        const { rememberMe } = this.state;

        return (
            <Grid textAlign='center' style={{ height: '100vh' }} verticalAlign='middle'>
                <Grid.Column style={{ maxWidth: 372 }}>
                    <Header as='h2' textAlign='center'>
                        Soulseek Web Example
                    </Header>
                    <Form size='large'>
                        <Segment>
                            <Form.Input 
                                fluid icon='user' 
                                iconPosition='left' 
                                placeholder='Username' 
                            />
                            <Form.Input
                                fluid
                                icon='lock'
                                iconPosition='left'
                                placeholder='Password'
                                type='password'
                            />
                            <Checkbox
                                label='Remember Me'
                                onChange={this.toggleRememberMe}
                                checked={rememberMe}
                            />
                        </Segment>
                        <Button 
                                primary 
                                fluid 
                                size='large'
                                className='login-button'
                                onClick={onLoginAttempt}
                            >
                                <Icon name='sign in'/>
                                Login
                            </Button>
                    </Form>
                </Grid.Column>
            </Grid>
        )
    }
}

export default LoginForm